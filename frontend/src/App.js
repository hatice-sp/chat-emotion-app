import React, { useState, useEffect, useRef } from "react";
import "./App.css";

const API_URL = process.env.REACT_APP_API_URL || "http://localhost:5128/api/Message";

function App() {
  const [messages, setMessages] = useState([]);
  const [messageText, setMessageText] = useState("");
  const [currentUserId, setCurrentUserId] = useState(1);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const chatEndRef = useRef(null);

  const users = [
    { id: 1, name: "Ahmet", color: "#3b82f6" },
    { id: 2, name: "Ayşe", color: "#ec4899" },
    { id: 3, name: "Mehmet", color: "#10b981" },
    { id: 4, name: "Zeynep", color: "#8b5cf6" },
  ];

  useEffect(() => {
    fetchMessages();
  }, []);

  useEffect(() => {
    // Yeni mesaj geldiğinde scroll en alta gitsin
    chatEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages]);

  const fetchMessages = async () => {
    try {
      setError(null);
      const res = await fetch(API_URL);
      if (!res.ok) throw new Error("Backend hatası");
      const data = await res.json();
      setMessages(data);
    } catch (err) {
      console.error(err);
      setError("Mesajlar yüklenemedi. Backend çalışıyor mu?");
    }
  };

  const sendMessage = async () => {
    if (!messageText.trim()) return;

    setLoading(true);
    setError(null);

    try {
      const res = await fetch(API_URL, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userId: currentUserId, text: messageText }),
      });
      if (!res.ok) throw new Error("Mesaj gönderilemedi");
      const newMessage = await res.json();
      setMessages([...messages, newMessage]);
      setMessageText("");
    } catch (err) {
      console.error(err);
      setError("Mesaj gönderilemedi! Backend çalışıyor mu?");
    } finally {
      setLoading(false);
    }
  };

  const getSentimentText = (sentiment) => {
    if (!sentiment) return "Belirsiz";
    switch (sentiment.toLowerCase()) {
      case "positive":
        return "Pozitif";
      case "negative":
        return "Negatif";
      default:
        return "Belirsiz";
    }
  };

  const currentUser = users.find((u) => u.id === currentUserId);

  return (
    <div className="app-container">
      <h1>💬 Emotion Chat</h1>

      <div className="user-selector">
        {users.map((u) => (
          <button
            key={u.id}
            onClick={() => setCurrentUserId(u.id)}
            className={currentUserId === u.id ? "active" : ""}
            style={{ backgroundColor: u.color }}
          >
            {u.name}
          </button>
        ))}
        <button onClick={fetchMessages} style={{ backgroundColor: "#9ca3af" }}>
          Yenile
        </button>
      </div>

      {error && <div className="error-message">{error}</div>}

      <div className="chat-area">
        {messages.map((msg) => {
          const user = users.find((u) => u.id === msg.userId);
          return (
            <div key={msg.id} className="message-item">
              <div className="message-header">
                <strong style={{ color: user?.color }}>{user?.name || "Kullanıcı"}</strong>
                <span>ID: {msg.userId}</span>
              </div>
              <div className="message-text">{msg.text}</div>
              <div className="message-sentiment">
                Duygu: {getSentimentText(msg.sentiment)}
              </div>
            </div>
          );
        })}
        <div ref={chatEndRef} />
      </div>

      <div className="input-area">
        <input
          type="text"
          value={messageText}
          onChange={(e) => setMessageText(e.target.value)}
          onKeyPress={(e) => e.key === "Enter" && !loading && sendMessage()}
          placeholder={`${currentUser?.name} olarak mesaj yazın...`}
        />
        <button onClick={sendMessage} disabled={loading}>
          {loading ? "Gönderiliyor..." : "Gönder"}
        </button>
      </div>
    </div>
  );
}

export default App;
