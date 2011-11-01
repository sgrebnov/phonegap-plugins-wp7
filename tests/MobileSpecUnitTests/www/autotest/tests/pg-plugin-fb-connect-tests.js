// replace this with your own APP_ID
var APP_ID = '311961255484993';

var init = function() {
    document.addEventListener("deviceready", function () {
        deviceReady = true;
        console.log("Device=" + device.platform + " " + device.version);

        FB.init({ appId: APP_ID, nativeInterface: PG.FB });

    }, false);
    window.setTimeout(function () {
        if (!deviceReady) {
            console.log("Error: PhoneGap did not initialize.  Tests will not run correctly.");
        }
    }, 1000);
}();

test('login', function loginToFacebook() {

    QUnit.stop(40000);
    expect(1);

    FB.login(function (response) {
        if (response.session) {
            QUnit.start()
            ok(true, 'user can login')            
        }
    }, { perms: "email read_stream publish_stream offline_access" })
})

test('get friends', function () {

    QUnit.stop(20000);
    expect(1);

    FB.api('/me/friends', function (response) {
        if (response.error) {
            console.log(JSON.stringify(response.error));
        } else {
            QUnit.start();
            var friends = response.data;            
            ok(friends, 'returned friends array')
            console.log('found ' + friends.length + ' friends!')
            for (var i = 0, l = friends.length; i < l; i++) {
                var id = friends[i].id
            , name = friends[i].name
                console.log(name)
            }
        }
    });
})

test('logout',function() {
    
    QUnit.stop(20000);
    expect(1);

    FB.logout(function (e) {
        QUnit.start()
        ok(true, 'user can logout')
    });
}) 
