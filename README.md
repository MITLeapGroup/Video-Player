# Video Player

A simple video player prefab with captions, progress bar, and playback controls.

## Installation

This package requires several dependencies to function correctly:

- **TextMeshPro** (`com.unity.textmeshpro`) – included in Unity Registry
- **OpenAI Unity Package** (`io.openai.unity`) – by Stephen Hodgson, available on [OpenUPM](https://openupm.com/packages/io.openai.unity/)
- **Utilities packages** (`com.utilities.*`) – available on OpenUPM

### Using OpenUPM

To ensure Unity can automatically resolve the OpenAI and Utilities packages, your project needs to include OpenUPM as a scoped registry. In your project's `Packages/manifest.json`, add:

```json
"scopedRegistries": [
{
    "name": "OpenUPM",
    "url": "https://package.openupm.com",
    "scopes": [
      "com.openai",
      "com.openai.unity",
      "com.utilities",
      "com.utilities.async",
      "com.utilities.audio",
      "com.utilities.encoder.wav",
      "com.utilities.extensions",
      "com.utilities.rest",
      "com.utilities.websockets"
    ]
  }
]
