completion_connected = false;

// helper : gotoState
function gotoState(newstate) {
    state = newstate;
    switch (state) {
        case 'login': showPart('.js-login'); break;
        case 'edit':
            {
                if ($('#cred-name-box')[0].value == "Security Manager")
                    $('.js-pin-div').removeClass('collapse'); else
                    $('.js-pin-div').addClass('collapse');

                showPart('.js-cred-edit'); $('#cred-name-box').focus();

                if (!completion_connected) {
                    completion_connected = true;

                    new Awesomplete($('#cred-name-box')[0], {
                        minChars: 1,
                        list: _.map(_.filter(aliases, (x) => x.name != null), (x) => x.name)
                    });

                    new Awesomplete($('#cred-username-box')[0], {
                        minChars: 1,
                        list: _.map(_.filter(aliases, (x) => x.username != null), (x) => x.username)
                    });

                    new Awesomplete($('#cred-email-box')[0], {
                        minChars: 1,
                        list: _.map(_.filter(aliases, (x) => x.email != null), (x) => x.email)
                    });
                }
            }
            break;
        case 'list': showPart('.js-main'); break;
    }
}

// save password ( if debug mode )
function savePassword() {
    if (debugmode) {
        sessionStorage.setItem('password', pwd);
        sessionStorage.setItem('pin', pin);
    }
}

// helper : shortPart
function showPart(jFilter) {
    $('.js-part').addClass('collapse');
    $(jFilter).removeClass('collapse');
}

// helper : checkApiError
function checkApiError(data) {
    if (data && data.exitCode == 1) {
        $.notify('error: ' + data.errorMsg, 'error');
        return true;
    }
    return false;
}

// helper : checkApiSuccessful
function checkApiSuccessful(data) {
    return data && data.exitCode == 0;
}

// helper : checkApiInvalidAuth
function checkApiInvalidAuth(data) {
    return data && data.exitCode == 2;
}

// helper : checkPin
function checkPin() {
    $('#pin-mask').html('<i style="font-size:4px" class="fas fa-circle"></i> '.repeat(pin.length));
    if (pin.length >= 4) {
        pwd = $('#login-password-box')[0].value;

        $.post(urlbase + '/Api/IsAuthValid',
            {
                password: pwd,
                pin: pin
            },
            function (data, status, jqXHR) {
                if (checkApiError(data)) return;
                if (checkApiSuccessful(data)) {
                    $.notify('logged in', 'success');
                    savePassword();
                    showPart('.js-main');
                    reloadCredShortList();
                    reloadAliases();
                }
                else {
                    $.notify('invalid login', 'error');
                    pin = '';
                    $('#pin-mask').text("");
                }
            });
    }
}

// handle pin
for (i = 0; i <= 9; ++i) {
    let x = i;
    $('#pin' + x + '-btn').click(function (e) {
        if (pin.length == 4) pin = '';
        pin += new String(x); checkPin();
    });
}

// buildCredObj
function buildCredObj() {
    return {
        GUID: $('#cred-guid')[0].value,
        Name: $('#cred-name-box')[0].value,
        Url: $('#cred-link-box')[0].value,
        Username: $('#cred-username-box')[0].value,
        Email: $('#cred-email-box')[0].value,
        Password: $('#cred-pass-box')[0].value,
        Pin: $('#cred-pin-box')[0].value,
        PasswordRegenLength: $('#cred-password-regen-length-box')[0].value,
        Notes: $('#cred-notes-box')[0].value
    };
}

// isEmptyCredObj
function isEmptyCredObj() {
    return $.trim($('#cred-name-box')[0].value) == "" &&
        $.trim($('#cred-link-box')[0].value) == "" &&
        $.trim($('#cred-username-box')[0].value) == "" &&
        $.trim($('#cred-email-box')[0].value) == "" &&
        $.trim($('#cred-pass-box')[0].value) == "" &&
        $.trim($('#cred-pin-box')[0].value) == "" &&
        $.trim($('#cred-password-regen-length-box')[0].value) == "" &&
        $.trim($('#cred-notes-box')[0].value) == "";

}