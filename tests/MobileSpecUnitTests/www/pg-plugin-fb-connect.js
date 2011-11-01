// TODO perm support, error handling

PG = ( typeof PG == 'undefined' ? {} : PG );
PG.FB = {
    init: function (apiKey) {
        console.log("fb-connect init");

        // create the fb-root element if it doesn't exist
        if (!document.getElementById('fb-root')) {
            var elem = document.createElement('div');
            elem.id = 'fb-root';
            document.body.appendChild(elem);
        }

        FB.fbConnect = FBConnect.install();
        FB.fbConnect.apiKey = apiKey;

        var session = JSON.parse(localStorage.getItem('pg_fb_session') || '{"expires":0}');
        console.log(JSON.stringify(session));
        if (session && session.expires > new Date().valueOf()) {
            FB.Auth.setSession(session, 'connected');
            FB.fbConnect.session = session;
        }
    },
    login: function (a, b) {
        console.log("fb-connect login");

        b = b || { perms: '' };

        FB.fbConnect.connect(FB.fbConnect.apiKey, "http://www.facebook.com/connect/login_success.html");
        FB.fbConnect.onConnect = function (e) {
            localStorage.setItem('pg_fb_session', JSON.stringify(e.session));
            FB.Auth.setSession(FB.fbConnect.session, 'connected');
            if (a) a(e);
        }
    },
    logout: function (cb) {
        console.log("fb-connect logout");
        FB.fbConnect.logout("http://www.facebook.com/connect/login_success.html");
        FB.fbConnect.onDisconnect = function (e) {
            localStorage.removeItem('pg_fb_session');
            FB.Auth.setSession(null, 'notConnected');
            if (cb) cb(e);
        }
    },
    getLoginStatus: function (cb) {
        console.log("fb-connect getLoginStatus");

        if (FB.fbConnect.session && FB.fbConnect.session.expires > new Date().valueOf()) {
            FB.fbConnect.status = "connected";
        }
        else {
            FB.fbConnect.status = "unknown";
        }

        if (cb) cb(FB.fbConnect);
    }
};

function FBConnect() {

    this.resetSession();

    if (window.plugins.childBrowser == null) {
        ChildBrowser.install();
    }

}

FBConnect.prototype.resetSession = function () {
    this.status = "unknown";
    this.session = {};
    this.session.access_token = null;
    this.session.expires = 0;
    this.session.secret = null;
    this.session.session_key = null;
    this.session.sig = null;
    this.session.uid = null;
}

FBConnect.prototype.connect = function (client_id, redirect_uri, display) {
    this.client_id = client_id;
    this.redirect_uri = redirect_uri;
    
    var authorize_url = "https://graph.facebook.com/oauth/authorize?";
    authorize_url += "client_id=" + client_id;
    authorize_url += "&redirect_uri=" + redirect_uri;
    authorize_url += "&display=" + (display ? display : "touch");
    authorize_url += "&type=user_agent";

    window.plugins.childBrowser.showWebPage(authorize_url);
    var self = this;
    window.plugins.childBrowser.onLocationChange = function (loc) { self.onLocationChange(loc); };
}

FBConnect.prototype.onLocationChange = function (newLoc) {
    if (newLoc.indexOf(this.redirect_uri) == 0) {
        var result = unescape(newLoc).split("#")[1];
        result = unescape(result);

        // TODO: Error Check
        this.session.access_token = result.split("&")[0].split("=")[1];
        var expiresIn = parseInt(result.split("&")[1].split("=")[1]);
        this.session.expires = new Date().valueOf() + expiresIn * 1000;
        this.status = "connected";

        window.plugins.childBrowser.close();
        this.onConnect(this);

    }
}

FBConnect.prototype.logout = function (redirect_uri) {
    this.redirect_uri = redirect_uri;
    var authorize_url = "https://www.facebook.com/logout.php?";
    authorize_url += "&next=" + redirect_uri;
    authorize_url += "&access_token=" + this.session.access_token;
    console.log("logout url: " + authorize_url);
    window.plugins.childBrowser.showWebPage(authorize_url);
    var self = this;
    window.plugins.childBrowser.onLocationChange = function (loc) {
        console.log("onLogout");
        window.plugins.childBrowser.close();
        self.resetSession();
        self.status = "notConnected";
        self.onDisconnect(this);
    };
}


FBConnect.prototype.getFriends = function () {
    var url = "https://graph.facebook.com/me/friends?access_token=" + this.accessToken;
    var req = new XMLHttpRequest();

    req.open("get", url, true);
    req.send(null);
    req.onerror = function () { alert("Error"); };
    return req;
}

// Note: this plugin does NOT install itself, call this method some time after deviceready to install it
// it will be returned, and also available globally from window.plugins.fbConnect
FBConnect.install = function () {
    if (!window.plugins) {
        window.plugins = {};
    }
    window.plugins.fbConnect = new FBConnect();
    return window.plugins.fbConnect;
}