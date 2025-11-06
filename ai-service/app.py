from flask import Flask, request, jsonify
import requests
import json
import time
import uuid

app = Flask(__name__)

SPACE_URL = "https://hatice10-chat-emotion-ai.hf.space"
API_PREFIX = "/gradio_api"

@app.route("/", methods=["GET"])
def home():
    return jsonify({"status": "Flask is running!"})

@app.route("/analyze", methods=["POST"])
def analyze():
    try:
        data = request.json
        text = data.get("text", "")
        
        if not text:
            return jsonify({"error": "Text required"}), 400
        
        print(f"\n{'='*50}")
        print(f"Analyzing: {text}")
        print(f"{'='*50}\n")
        
        # Session hash oluştur
        session_hash = str(uuid.uuid4())
        
        # 1. Queue'ya katıl
        join_url = f"{SPACE_URL}{API_PREFIX}/queue/join"
        
        join_payload = {
            "data": [text],
            "event_data": None,
            "fn_index": 2,  # Config'den: dependencies id=2 "predict" fonksiyonu
            "trigger_id": 2,
            "session_hash": session_hash
        }
        
        print(f"Step 1: Joining queue...")
        print(f"URL: {join_url}")
        print(f"Payload: {json.dumps(join_payload, indent=2)}")
        
        join_response = requests.post(join_url, json=join_payload, timeout=10)
        
        print(f"Join Status: {join_response.status_code}")
        print(f"Join Response: {join_response.text}\n")
        
        if join_response.status_code != 200:
            return jsonify({
                "error": "Failed to join queue",
                "status": join_response.status_code,
                "response": join_response.text
            }), 500
        
        join_data = join_response.json()
        event_id = join_data.get("event_id")
        
        if not event_id:
            return jsonify({
                "error": "No event_id in response",
                "response": join_data
            }), 500
        
        print(f"Event ID: {event_id}\n")
        
        # 2. SSE stream'den sonucu al
        data_url = f"{SPACE_URL}{API_PREFIX}/queue/data"
        
        params = {
            "session_hash": session_hash
        }
        
        print(f"Step 2: Listening for results...")
        print(f"URL: {data_url}")
        print(f"Params: {params}\n")
        
        stream_response = requests.get(
            data_url, 
            params=params, 
            stream=True, 
            timeout=60,
            headers={"Accept": "text/event-stream"}
        )
        
        # SSE stream'i oku
        for line in stream_response.iter_lines():
            if line:
                line_text = line.decode('utf-8')
                print(f"Stream line: {line_text}")
                
                # SSE formatı: "data: {...}"
                if line_text.startswith('data: '):
                    try:
                        event_json = json.loads(line_text[6:])
                        msg_type = event_json.get("msg")
                        
                        print(f"Message type: {msg_type}")
                        
                        if msg_type == "process_completed":
                            # Sonuç geldi
                            output_data = event_json.get("output", {}).get("data", [])
                            
                            print(f"\n{'='*50}")
                            print(f"SUCCESS! Raw output: {output_data}")
                            print(f"{'='*50}\n")
                            
                            if output_data and len(output_data) > 0:
                                result = output_data[0]
                                
                                # Result bir dict ise
                                if isinstance(result, dict):
                                    return jsonify({
                                        "sentiment": result.get("sentiment") or result.get("label") or result.get("duygu") or "unknown",
                                        "confidence": float(result.get("confidence") or result.get("score") or result.get("güven") or 0),
                                        "raw_result": result
                                    })
                                
                                # Result string ise
                                else:
                                    return jsonify({
                                        "result": result,
                                        "raw_result": result
                                    })
                        
                        elif msg_type == "estimation":
                            rank = event_json.get("rank")
                            queue_size = event_json.get("queue_size")
                            print(f"Queue position: {rank}/{queue_size}")
                        
                        elif msg_type == "progress":
                            progress_data = event_json.get("progress_data", [])
                            print(f"Progress: {progress_data}")
                        
                        elif msg_type == "process_starts":
                            print("Process started...")
                        
                    except json.JSONDecodeError as e:
                        print(f"JSON decode error: {e}")
                        continue
        
        return jsonify({
            "error": "Stream ended without result"
        }), 500
        
    except requests.exceptions.Timeout:
        return jsonify({
            "error": "Request timeout"
        }), 504
    except Exception as e:
        import traceback
        error_detail = traceback.format_exc()
        print(f"\n{'='*50}")
        print(f"ERROR:\n{error_detail}")
        print(f"{'='*50}\n")
        
        return jsonify({
            "error": str(e),
            "type": type(e).__name__
        }), 500

if __name__ == "__main__":
    print("\n" + "="*60)
    print(" Flask App Started!")
    print("="*60)
    print("\n Available Routes:")
    print("  GET  http://127.0.0.1:5000/")
    print("  POST http://127.0.0.1:5000/analyze")
    print("\n" + "="*60 + "\n")
    app.run(host="0.0.0.0", port=5000, debug=True)