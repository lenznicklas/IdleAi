namespace IdleAi;

/// <summary>
/// Firebase values for Idle AI.
///
/// These values come from the Firebase Android app registered in the same
/// Google Cloud project as Idle AI Play Games Services. The Play Games web
/// client id must be the exact same Web application OAuth client configured in:
/// Firebase Authentication -> Play Games
/// and
/// Play Console -> Play Games Services -> Game server credential.
///
/// Client IDs and Firebase Web API keys are identifiers, not client secrets.
/// Never put the OAuth client secret into the game.
/// </summary>
public static class FirebaseConfig
{
	public const string ProjectId =
		"idle-ai-empire";

	public const string ProjectNumber =
		"750491737070";

	public const string WebApiKey =
		"AIzaSyBvFRNihylakIMg8SkFf_Vhidnqg7bh9SU";

	public const string AndroidPackageName =
		"com.lenznicklas.idleai";

	public const string MobileSdkAppId =
		"1:750491737070:android:9f2c65ebccf2c5214f8cff";

	public const string PlayGamesWebClientId =
		"750491737070-8m7mgojsbenc5h0vlcmocp9h3qur2e99.apps.googleusercontent.com";

	public const string AuthRequestUri =
		"http://localhost";
}
