# WardrobeIQ API

Backend for **WardrobeIQ — Dress with Intelligence**, an AI-powered personal styling application.

## Overview

WardrobeIQ analyzes a user's face shape, body shape, and skin undertone, then combines that with their digital wardrobe to generate personalized, occasion-based outfit suggestions and styling guidance.

## Features

- 🔐 **Authentication** — JWT-based auth with BCrypt password hashing
- 👕 **Wardrobe Management** — Full CRUD for `ClothingItem`, `Outfit`, and `WornLog`
- 📐 **Face Shape Calculator** — Measurement-based classification
- 📏 **Body Shape Calculator** — Measurement-based classification
- 🎨 **Skin Undertone Analyzer** — Photo-based analysis using Gemini vision
- 🧠 **AI Outfit Suggestions** — Occasion-based recommendations combining structured occasion types, wardrobe data, and face/body/undertone results
- 💡 **Styling Guide** — General guidance on necklines, hairstyles, sleeves, silhouettes, colors, and items to avoid
- ✅ **Reliability** — Input validation and retry logic for transient errors (e.g. AI service calls)

## Tech Stack

- **Framework:** ASP.NET Core
- **Database:** MongoDB
- **AI/Vision:** Gemini API
- **Auth:** JWT + BCrypt

## Getting Started

### Prerequisites

- .NET SDK (version used by this project)
- MongoDB instance (local or Atlas)
- Gemini API key ([ai.google.dev](https://ai.google.dev))

### Setup

1. Clone the repository

   ```bash
   git clone https://github.com/Sandali-82/WardrobeIQ-API.git
   cd WardrobeApi
   ```

2. Copy the example config and fill in your own values:

   ```bash
   cp appsettings.Example.json appsettings.Development.json
   ```

   `appsettings.Development.json` is gitignored and never committed — it holds your real secrets locally:

   ```json
   {
     "MongoDb": {
       "ConnectionString": "<your-mongodb-connection-string>",
       "DatabaseName": "WardrobeIQ"
     },
     "Jwt": {
       "Key": "<a-long-random-secret-32-chars-or-more>",
       "Issuer": "WardrobeIQ",
       "Audience": "WardrobeIQ"
     },
     "Gemini": {
       "ApiKey": "<your-gemini-api-key>"
     }
   }
   ```

3. Restore dependencies and run:

   ```bash
   dotnet restore
   dotnet run
   ```

4. The API will be available at `https://localhost:<port>` (see console output for the exact port), with Swagger UI at `/swagger` in development.

## Configuration Files

| File | Committed? | Purpose |
|---|---|---|
| `appsettings.json` | ✅ Yes | Base config, placeholder values only |
| `appsettings.Example.json` | ✅ Yes | Template showing the expected shape for local secrets |
| `appsettings.Development.json` | ❌ No (gitignored) | Real local secrets (Mongo connection string, JWT key, Gemini API key) |

## API Testing

All endpoints have been manually tested via Postman, covering the full flow: auth → wardrobe CRUD → outfit/worn-log → AI suggestions → face/body shape → undertone analysis → styling guide.

*(Optional: export your Postman collection as JSON, commit it to the repo, and link it here so others can import and test it directly.)*

## Related Repositories

- 📱 **Mobile app (Flutter):** `[<link to wardrobeiq-app repo>](https://github.com/Sandali-82/WardrobeIQ-App.git)`

## License

MIT License

Copyright (c) 2026 Sandali Kodippili

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
