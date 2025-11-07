Chat Emotion App
Kullanıcıların mesajlaşarak sohbet edebildiği ve AI tarafından duygu analizi yapılan bir web uygulaması. React, .NET 8 ve Python AI servisleri ile geliştirilmiştir.
Klasör Yapısı
chat-emotion-app/
├── frontend/           # React web uygulaması
├── backend/            # .NET 8 Web API
├── ai-service/         # Python duygu analizi servisi
└── README.md
Kurulum
Backend (.NET 8)
cd backend/ChatEmotionBackend
dotnet restore
dotnet run
API: http://localhost:5000

AI Servisi (Python)
cd ai-service
python -m venv venv
venv\Scripts\activate       # Windows
pip install -r requirements.txt
python sentiment_analysis.py
AI Servisi: http://localhost:5001

Frontend (React)
cd frontend
npm install
npm start
Uygulama: http://localhost:3000
Deployment

Frontend: Vercel ile deploy edilebilir
Backend: Render ile deploy edilebilir
AI Servisi: Render ile deploy edilebilir

Teknolojiler

Frontend: React 18, JavaScript
Backend: .NET 8, C#
AI: Python 3.11, Hugging Face Transformers
Database: SQL 

AI Kullanımı
AI yalnızca mesajların duygu analizini yapar (positive, negative, neutral). Uygulamanın tüm mantığı, API çağrıları, veritabanı işlemleri ve UI kodları elle yazılmıştır.
