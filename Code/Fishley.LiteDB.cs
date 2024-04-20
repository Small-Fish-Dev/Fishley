public partial class Fishley
{
	public static string DatabasePath => ConfigGet<string>( "Database" );
	public static LiteDatabase Database => new LiteDatabase( DatabasePath );
	public static ILiteCollection<User> Users => Database.GetCollection<User>( "users" );

	public class User
	{
		[BsonId]
		public ulong UserId {get; set; } = 0;
		public int Warnings { get; set; } = 0;
		public long LastWarn { get; set; } = 0;
		public decimal Money { get; set; } = 0.00m;
		public long LastFish { get; set; } = 0;

		public User() {}

		public User( ulong userId )
		{
			UserId = userId;
		}
	}

	public static User UserGet( ulong userId )
	{
		var user = Users.Find( x => x.UserId == userId )
		.FirstOrDefault();

		if ( user != null )
			return user;
		else
		{
			user = new User( userId );
			UserUpdate( user );
			return user;
		}
	}
	public static bool UserExists( ulong userId ) => Users.Exists( x => x.UserId == userId );
	public static bool UserExists( User user ) => UserExists( user.UserId );
	public static void UserUpdate( User user )
	{
		using ( var database = new LiteDatabase( DatabasePath ) )
		{
			database.BeginTrans();
			var users = Database.GetCollection<User>( "users" );
	 		users.Upsert( user );
			database.Commit();
		}
	}
	
	public static void UserDelete( ulong userId )
	{
		if ( UserExists( userId ) )
			Users.Delete( userId );
	}

	public static void UserDelete( User user ) => UserDelete( user.UserId );
}