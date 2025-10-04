# SpotifyAndFeel

SpotifyAndFeel

SpotifyAndFeel is a lightweight desktop application built with C# and WPF that connects to the Spotify Web API.
It allows users to authenticate with Spotify, search for tracks, and play them directly on their active Spotify device.
The app uses a local redirect server to handle Spotify’s OAuth 2.0 authorization process.

Features

Spotify OAuth2 authentication using a local callback server

Track search and playback via the Spotify Web API

Automatically selects and plays the top search result

Simple user interface with background tray icon

Clean service-oriented architecture

Architecture Overview

App.xaml
├── AuthService.cs → Handles Spotify authorization by starting a temporary local web server for OAuth callback
├── TokenService.cs → Exchanges the authorization code for access and refresh tokens
├── SpotifyApiService.cs → Communicates with the Spotify Web API (search, playback, etc.)
└── Models/ → Contains configuration and response models such as SpotifyConfig.cs and TokenResponse.cs

Setup Instructions
1. Create a Spotify Developer Application

Go to https://developer.spotify.com/dashboard

