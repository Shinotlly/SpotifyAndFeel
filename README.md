🎵 SpotifyAndFeel

!!!⚠️ You need Spotify Premium to use this application.!!!

SpotifyAndFeel is a smart desktop application that combines Spotify control with speech recognition.

You can:

🎤 Speak in your selected language and let the app convert your voice to text

🔍 Automatically search and play matching songs on your Spotify account

⌨️ Or simply type text to search and play music

📝 If speech recognition doesn’t work well, you can manually edit the recognized text

Example usage:

“I feel so tired today” → The app searches and plays a matching song automatically 🎶

-----✨ Features-----

🎧 Spotify playback control via Spotify Web API

🎤 Speech-to-text support (English & Turkish)

⌨️ Text-based search support

-----🔑 Spotify API Setup-----

Go to:
👉 https://developer.spotify.com/

Open your Dashboard and create a new app.

You will get:

Client ID

Client Secret

Add this Redirect URI in your Spotify app settings:

http://127.0.0.1:5000/callback

-----⚙️ Configuration-----

1️⃣ Create appsettings.json

In the project root folder, create a file named:

appsettings.json

And put this inside:

{
  "Spotify": {
    "ClientId": "your-client-id-here",
    "ClientSecret": "your-client-secret-here",
    "RedirectUriBase": "http://127.0.0.1"
  }
}

2️⃣ Restore NuGet Packages

Run:

dotnet restore

-----🗣️ Speech Recognition Models (Vosk)-----

This project uses Vosk speech recognition models for:

🇺🇸 English

🇹🇷 Turkish

Download models from:

👉 https://alphacephei.com/vosk/models

Download:

vosk-model-small-en-us-0.15

vosk-model-small-tr-0.3

Then:

Extract them

Copy them into the Models folder in the project

⚠️ You can change models, but be careful not to break the code.

-----⌨️ Hotkey Notice-----

⚠️ When using a hotkey combination, release the modifier key (e.g., Ctrl) before releasing the main key to ensure correct behavior.

-----⚠️ Disclaimer-----

This project is not affiliated with, endorsed, or sponsored by Spotify.
It uses the Spotify Web API for educational and non-commercial purposes only.

-----📜 License-----

This project is open-source and provided under the MIT License.
