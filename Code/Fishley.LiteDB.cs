public partial class Fishley
{
	public static string DatabasePath => ConfigGet( "Database", "smallfish_database.db" );
	public static LiteDatabase Database { get; set; }
	public static ILiteCollection<User> Users => Database.GetCollection<User>( "users" );

	public class User
	{
		[BsonId]
		public ulong UserId {get; set; } = 0;
		public int Warnings { get; set; } = 0;
		public long LastWarn { get; set; } = 0;
		public decimal Money { get; set; } = 10.00M;

		public User() {}

		public User( ulong userId )
		{
			UserId = userId;
		}
	}

    private static void InitializeDatabase() => Database = new ( DatabasePath );

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
	public static void UserUpdate( User user ) => Users.Upsert( user );
	
	public static void UserDelete( ulong userId )
	{
		if ( UserExists( userId ) )
			Users.Delete( userId );
	}

	public static void UserDelete( User user ) => UserDelete( user.UserId );
}