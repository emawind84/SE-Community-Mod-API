namespace SEModAPIInternal.Support
{
	using System;
	using System.IO;
	using System.Reflection;
	using System.Text;
	using System.Threading;
	using SEModAPIInternal.API.Common;
	using SysUtils.Utils;
	using VRage.Common.Utils;

	public class ApplicationLog
	{
		#region "Attributes"

		private bool m_useInstancePath;
		private bool m_instanceMode;
		private string m_logFileName;
		private StringBuilder m_appVersion;
		private DirectoryInfo m_libraryPath;
		private FileInfo m_filePath;
		private StringBuilder m_stringBuilder;
		private static readonly object _logLock = new object( );

		#endregion "Attributes"

		#region "Constructors and Initializers"

		public ApplicationLog( bool useGamePath = false )
		{
			m_useInstancePath = useGamePath;

			if ( m_useInstancePath && SandboxGameAssemblyWrapper.Instance.IsGameStarted && MyFileSystem.UserDataPath != null )
			{
				m_libraryPath = new DirectoryInfo( MyFileSystem.UserDataPath );

				m_instanceMode = true;
			}
			else
			{
				string codeBase = Assembly.GetExecutingAssembly( ).CodeBase;
				UriBuilder uri = new UriBuilder( codeBase );
				string path = Uri.UnescapeDataString( uri.Path );
				m_libraryPath = new DirectoryInfo( Path.Combine( Path.GetDirectoryName( path ), "Logs" ) );
				if ( !m_libraryPath.Exists )
					Directory.CreateDirectory( m_libraryPath.ToString( ) );
			}

			m_stringBuilder = new StringBuilder( );
		}

		#endregion "Constructors and Initializers"

		#region "Properties"

		public bool LogEnabled
		{
			get { return m_filePath != null; }
		}

		#endregion "Properties"

		#region "Methods"

		public string GetFilePath( )
		{
			if ( m_filePath == null )
				return "";

			return m_filePath.ToString( );
		}

		public void Init( string logFileName, StringBuilder appVersionString )
		{
			m_logFileName = logFileName;
			m_appVersion = appVersionString;

			m_filePath = new FileInfo( Path.Combine( m_libraryPath.ToString( ), m_logFileName ) );

			//If the log file already exists then archive it
			if ( m_filePath.Exists )
			{
				DateTime lastWriteTime = m_filePath.LastWriteTime;
				string modifiedTimestamp = lastWriteTime.Year.ToString( ) + "_" + lastWriteTime.Month.ToString( ) + "_" + lastWriteTime.Day.ToString( ) + "_" + lastWriteTime.Hour.ToString( ) + "_" + lastWriteTime.Minute.ToString( ) + "_" + lastWriteTime.Second.ToString( );
				string fileNameWithoutExtension = m_filePath.Name.Remove( m_filePath.Name.Length - m_filePath.Extension.Length );
				string newFileName = fileNameWithoutExtension + "_" + modifiedTimestamp + m_filePath.Extension;

				File.Move( m_filePath.ToString( ), Path.Combine( m_libraryPath.ToString( ), newFileName ).ToString( ) );
			}

			int num = (int)Math.Round( ( DateTime.Now - DateTime.UtcNow ).TotalHours );

			WriteLine( "Log Started" );
			WriteLine( "Timezone (local - UTC): " + num.ToString( ) + "h" );
			WriteLine( "App Version: " + m_appVersion );
		}

		public void WriteLine( string msg )
		{
			if ( m_filePath == null )
				return;

			if ( m_useInstancePath && !m_instanceMode && SandboxGameAssemblyWrapper.Instance.IsGameStarted && MyFileSystem.UserDataPath != null )
			{
				m_libraryPath = new DirectoryInfo( MyFileSystem.UserDataPath );

				m_instanceMode = true;

				Init( m_logFileName, m_appVersion );
			}

			try
			{
				lock ( _logLock )
				{
					m_stringBuilder.Clear( );
					AppendDateAndTime( m_stringBuilder );
					m_stringBuilder.Append( " - " );
					AppendThreadInfo( m_stringBuilder );
					m_stringBuilder.Append( " -> " );
					m_stringBuilder.Append( msg );
					TextWriter m_Writer = new StreamWriter( m_filePath.ToString( ), true );
					TextWriter.Synchronized( m_Writer ).WriteLine( m_stringBuilder.ToString( ) );
					m_Writer.Close( );
					m_stringBuilder.Clear( );
				}
			}
			catch ( Exception ex )
			{
				Console.WriteLine( "Failed to write to log: " + ex.ToString( ) );
			}
		}

		public void WriteLine( string message, LoggingOptions option )
		{
			WriteLine( message );
		}

		public void WriteLine( Exception ex )
		{
			if ( m_filePath == null )
				return;

			if ( ex == null )
				return;

			WriteLine( ex.ToString( ) );

			if ( ex.InnerException == null )
				return;

			WriteLine( ex.InnerException );
		}

		public void WriteLineAndConsole( string msg )
		{
			if ( m_filePath == null )
				return;

			WriteLine( msg );

			lock ( _logLock )
			{
				m_stringBuilder.Clear( );
				AppendDateAndTime( m_stringBuilder );
				m_stringBuilder.Append( " - " );
				m_stringBuilder.Append( msg );
				Console.WriteLine( m_stringBuilder.ToString( ) );
				m_stringBuilder.Clear( );
			}
		}

		public void WriteLineAndConsole( Exception ex )
		{
			if ( m_filePath == null )
				return;

			WriteLine( ex );

			lock ( _logLock )
			{
				m_stringBuilder.Clear( );
				AppendDateAndTime( m_stringBuilder );
				m_stringBuilder.Append( " - " );
				m_stringBuilder.Append( ex );
				try
				{
					Console.WriteLine( m_stringBuilder.ToString( ) );
				}
				catch ( IOException ioex )
				{
					WriteLine( ioex );
				}
				m_stringBuilder.Clear( );
			}
		}

		private int GetThreadId( )
		{
			return Thread.CurrentThread.ManagedThreadId;
		}

		private void AppendDateAndTime( StringBuilder sb )
		{
			try
			{
				DateTimeOffset now = DateTimeOffset.Now;
				sb.Concat( now.Year, 4U, '0', 10U, false ).Append( '-' );
				sb.Concat( now.Month, 2U ).Append( '-' );
				sb.Concat( now.Day, 2U ).Append( ' ' );
				sb.Concat( now.Hour, 2U ).Append( ':' );
				sb.Concat( now.Minute, 2U ).Append( ':' );
				sb.Concat( now.Second, 2U ).Append( '.' );
				sb.Concat( now.Millisecond, 3U );
			}
			catch ( Exception ex )
			{
				Console.WriteLine( ex.ToString( ) );
			}
		}

		private void AppendThreadInfo( StringBuilder sb )
		{
			sb.Append( "Thread: " + GetThreadId( ).ToString( ) );
		}

		#endregion "Methods"
	}
}