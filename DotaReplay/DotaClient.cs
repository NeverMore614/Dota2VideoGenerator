using System;
using System.Collections.Generic;
using System.Threading;
using System.Text;

using SteamKit2;
using SteamKit2.Internal; // brings in our protobuf client messages
using SteamKit2.GC; // brings in the GC related classes
using SteamKit2.GC.Dota.Internal;
using System.Collections;
using MetaDota.Common; // brings in dota specific protobuf messages

namespace MetaDota.DotaReplay;

class DotaClient : SingleTon<DotaClient>
{

    Dictionary<uint, string> _heroNameDic;
    SteamClient client;

    SteamUser user;
    SteamGameCoordinator gameCoordinator;

    CallbackManager callbackMgr;
    ulong matchId;

    bool gotMatch;

    // setup our dispatch table for messages
    // this makes the code cleaner and easier to maintain
    Dictionary<uint, Action<IPacketGCMsg>> messageMap;

    string authCode = null, twoFactorAuth = null;

    public CMsgDOTAMatch? Match { get; private set; }

    public bool IsLogonDota = false;

    public static string dotaLauncherPath = "game/bin/win64/dota2.exe";

    public static string dotaMoviePath = "../movie";

    public static string dotaCfgPath = "game/dota/cfg";

    public static string dotaReplayPath = "game/dota/replays";

    public static string dotaPath = "";

    public bool IsInit = false;

    public DotaClient()
    {
        _init_hero_json();
        IsLogonDota = false;
        //this.matchId = matchId;

        client = new SteamClient();

        // get our handlers
        user = client.GetHandler<SteamUser>();
        gameCoordinator = client.GetHandler<SteamGameCoordinator>();

        // setup callbacks
        callbackMgr = new CallbackManager( client );

        callbackMgr.Subscribe<SteamClient.ConnectedCallback>( OnConnected );
        callbackMgr.Subscribe<SteamUser.LoggedOnCallback>( OnLoggedOn );
        callbackMgr.Subscribe<SteamGameCoordinator.MessageCallback>( OnGCMessage );
        callbackMgr.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);
        callbackMgr.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);
        messageMap = new Dictionary<uint, Action<IPacketGCMsg>>
            {
                { ( uint )EGCBaseClientMsg.k_EMsgGCClientWelcome, OnClientWelcome },
                { ( uint )EDOTAGCMsg.k_EMsgGCMatchDetailsResponse, OnMatchDetails },
            };
          
        //authCode = "";
        //MDFile.ReadLine("auth.txt", ref authCode);
    }

    /// <summary>
    /// 初始化调用，连接到dota2服务器
    /// </summary>
    public void Init(string dota2BetaPath)
    {
        IsInit = true;
        dotaPath = dota2BetaPath;
        dotaLauncherPath = Path.Combine(dotaPath, dotaLauncherPath);
        dotaMoviePath = Path.Combine(dotaPath, dotaMoviePath);
        dotaCfgPath = Path.Combine(dotaPath, dotaCfgPath);
        dotaReplayPath = Path.Combine(dotaPath, dotaReplayPath);
    }

    void _init_hero_json()
    {
        _heroNameDic = new Dictionary<uint, string>();
        SimpleJSON.JSONArray jsonArray = SimpleJSON.JSON.Parse(File.ReadAllText(System.AppDomain.CurrentDomain.BaseDirectory + "/dota2HeroJson.txt")).AsArray;
        foreach (var node in jsonArray.Childs)
        {
            _heroNameDic.Add(uint.Parse(node["id"]), node["name"]);
        }
    }

    public void Reconnect()
    { 
        client.Disconnect();
        Connect();
        WaitLogon();
    }

    void OnDisconnected(SteamClient.DisconnectedCallback callback)
    {
        // after recieving an AccountLogonDenied, we'll be disconnected from steam
        // so after we read an authcode from the user, we need to reconnect to begin the logon flow again

        Console.WriteLine("Disconnected from Steam, reconnecting in 5...");
        IsLogonDota = false;
        //Thread.Sleep(TimeSpan.FromSeconds(5));

        //steamClient.Connect();
    }

    void OnLoggedOff(SteamUser.LoggedOffCallback callback)
    {
        Console.WriteLine("Logged off of Steam: {0}", callback.Result);
        IsLogonDota = false;
    }

    public void Connect()
    {
        Console.WriteLine( "Connecting to Steam..." );

        // begin the connection to steam
        client.Connect();
    }

    public void WaitLogon()
    {
        while ( !gotMatch && !IsLogonDota )
        {
            // continue running callbacks until we get match details
            callbackMgr.RunWaitCallbacks( TimeSpan.FromSeconds( 1 ) );
            //await Task.Delay( 1000 );
            Console.WriteLine("WaitLogon...");
        }
    }

    public void WaitMatch()
    {
        while (!gotMatch)
        {
            // continue running callbacks until we get match details
            callbackMgr.RunWaitCallbacks(TimeSpan.FromSeconds(1));
            Console.WriteLine("gotMatch...");
        }
    }

    // called when the client successfully (or unsuccessfully) connects to steam
    void OnConnected( SteamClient.ConnectedCallback callback )
    {
        if (authCode != null)
        {
            Console.WriteLine("authCode = '{0}' into Steam...", authCode);
        }
        Console.WriteLine( "Connected! Logging '{0}' into Steam...", ClientParams.STEAM_USERNAME );

        // we've successfully connected, so now attempt to logon
        user.LogOn( new SteamUser.LogOnDetails
        {
            Username = ClientParams.STEAM_USERNAME,
            Password = ClientParams.STEAM_PASSWORD,
            // in this sample, we pass in an additional authcode
            // this value will be null (which is the default) for our first logon attempt
            AuthCode = authCode,

            // if the account is using 2-factor auth, we'll provide the two factor code instead
            // this will also be null on our first logon attempt
            TwoFactorCode = twoFactorAuth,
        } );
    }

    // called when the client successfully (or unsuccessfully) logs onto an account
    void OnLoggedOn( SteamUser.LoggedOnCallback callback )
    {

        bool isSteamGuard = callback.Result == EResult.AccountLogonDenied;
        bool is2FA = callback.Result == EResult.AccountLoginDeniedNeedTwoFactor;

        if (isSteamGuard || is2FA)
        {
            Console.WriteLine("This account is SteamGuard protected!");

            if (is2FA)
            {
                Console.WriteLine("Please enter your 2 factor auth code from your authenticator app: ");
                twoFactorAuth = Console.ReadLine();
            }
            else
            {
                Console.WriteLine("Please enter the auth code sent to the email at {0}: ", callback.EmailDomain);
                authCode = Console.ReadLine();
            }
            client.Connect();
            return;
        }
        if ( callback.Result != EResult.OK )
        {
            // logon failed (password incorrect, steamguard enabled, etc)
            // an EResult of AccountLogonDenied means the account has SteamGuard enabled and an email containing the authcode was sent
            // in that case, you would get the auth code from the email and provide it in the LogOnDetails

            Console.WriteLine( "Unable to logon to Steam: {0}", callback.Result );

            gotMatch = true; // we didn't actually get the match details, but we need to jump out of the callback loop
            return;
        }

        Console.WriteLine( "Logged in! Launching DOTA..." );

        // we've logged into the account
        // now we need to inform the steam server that we're playing dota (in order to receive GC messages)

        // steamkit doesn't expose the "play game" message through any handler, so we'll just send the message manually
        var playGame = new ClientMsgProtobuf<CMsgClientGamesPlayed>( EMsg.ClientGamesPlayed );

        playGame.Body.games_played.Add( new CMsgClientGamesPlayed.GamePlayed
        {
            game_id = new GameID( ClientParams.APPID ), // or game_id = APPID,
        } );

        // send it off
        // notice here we're sending this message directly using the SteamClient
        client.Send( playGame );

        // delay a little to give steam some time to establish a GC connection to us
        Thread.Sleep( 6000 );

        // inform the dota GC that we want a session
        var clientHello = new ClientGCMsgProtobuf<SteamKit2.GC.Dota.Internal.CMsgClientHello>( ( uint )EGCBaseClientMsg.k_EMsgGCClientHello );
        clientHello.Body.engine = ESourceEngine.k_ESE_Source2;
        gameCoordinator.Send( clientHello, ClientParams.APPID);
    }

    // called when a gamecoordinator (GC) message arrives
    // these kinds of messages are designed to be game-specific
    // in this case, we'll be handling dota's GC messages
    void OnGCMessage( SteamGameCoordinator.MessageCallback callback )
    {
        Action<IPacketGCMsg> func;
        if ( !messageMap.TryGetValue( callback.EMsg, out func ) )
        {
            // this will happen when we recieve some GC messages that we're not handling
            // this is okay because we're handling every essential message, and the rest can be ignored
            return;
        }

        func( callback.Message );
    }

    // this message arrives when the GC welcomes a client
    // this happens after telling steam that we launched dota (with the ClientGamesPlayed message)
    // this can also happen after the GC has restarted (due to a crash or new version)
    void OnClientWelcome( IPacketGCMsg packetMsg )
    {
        // in order to get at the contents of the message, we need to create a ClientGCMsgProtobuf from the packet message we recieve
        // note here the difference between ClientGCMsgProtobuf and the ClientMsgProtobuf used when sending ClientGamesPlayed
        // this message is used for the GC, while the other is used for general steam messages
        var msg = new ClientGCMsgProtobuf<CMsgClientWelcome>( packetMsg );

        Console.WriteLine( "GC is welcoming us. Version: {0}", msg.Body.version );

        IsLogonDota = true;


        // at this point, the GC is now ready to accept messages from us
        // so now we'll request the details of the match we're looking for


    }

    public void RequestMatch(ulong match_id)
    {
        Console.WriteLine( "Requesting details of match {0}", match_id);
        Match = null;
        gotMatch = false;
        var requestMatch = new ClientGCMsgProtobuf<CMsgGCMatchDetailsRequest>( ( uint )EDOTAGCMsg.k_EMsgGCMatchDetailsRequest );
        requestMatch.Body.match_id = match_id;
        
        gameCoordinator.Send( requestMatch, ClientParams.APPID);
    }

    // this message arrives after we've requested the details for a match
    void OnMatchDetails( IPacketGCMsg packetMsg )
    {
        var msg = new ClientGCMsgProtobuf<CMsgGCMatchDetailsResponse>( packetMsg );
        gotMatch = true;
        EResult result = ( EResult )msg.Body.result;
        if ( result != EResult.OK )
        {
            Console.WriteLine( "Unable to request match details: {0}", result );
            return;
        }
        Match = msg.Body.match;
        // we've got everything we need, we can disconnect from steam now
        //client.Disconnect();
    }

    public string GetHeroNameByID(uint id)
    {
        string name;
        if (_heroNameDic.TryGetValue(id, out name))
        {
            return name;
        }
        return "";
    }
}
