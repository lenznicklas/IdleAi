namespace IdleAi;

/// <summary>
/// Firebase values for Idle AI.
///
/// Project id and Web API key were generated from the project's
/// google-services.json. The Play Games web client id must be the exact same
/// "Web application" OAuth client that is configured in:
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
		"idle-ai-empire-57f8d";

	public const string WebApiKey =
		"AIzaSyBid-iP6CAYMXrHT5GiFDCuEoZgXXx1mJs";

	public const string AndroidPackageName =
		"com.lenznicklas.idleai";

	public const string MobileSdkAppId =
		"1:720727343504:android:8f7fbac37db798db3bf315";

	public const string PlayGamesWebClientId =
		"750491737070-1g8ngv995rs4q56rlq70qmus841jrsel.apps.googleusercontent.com";

	public const string AuthRequestUri =
		"http://localhost";
}
