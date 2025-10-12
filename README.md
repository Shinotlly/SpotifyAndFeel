# SpotifyAndFeel

(You need Spotify Premium for this app)

SpotifyAndFeel is a smart desktop application that combines Spotify control with speech recognition.
It allows you to speak or type a song name or just random words (for example: I feel soo tired today) and instantly play it on your Spotify account.
And if voice rec didnt work well, you can change the text by manuel.
And also if you want to search with any language, you can just type your words and press Play button.

First you have to sign in to Spotify Api service (You need Spotfiy Premium). Go to your dashboard and create an app. ---> (https://developer.spotify.com/)
Then you will see your Client ID and Client secret. And also you need a Redirect URI. Copy and paste this http for that ---> (http://127.0.0.1:5000/callback)
On appsettings.json (on solution explorer), you have to type your client id and client secret that you saw on your dashboard.

Use the .NET CLI to restore all NuGet packages: dotnet restore

This project uses open-source Vosk speech recognition models for English and Turkish.

https://alphacephei.com/vosk/models go to this link and download English (vosk-model-small-en-us-0.15) and Turkish (vosk-model-small-tr-0.3) models (necessary, if you dont want to use one of this models you can change them but try not to break the code)
And unzip this models, add to Models (on solution explorer). 
