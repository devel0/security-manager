urlbase = 'http://localhost:5000'; // set to external url from entrypoint
debugmode = true; // set to false from entrypoint

pwd = '';
pin = '';
if (debugmode) {
    pwd = sessionStorage.getItem('password');
    pin = sessionStorage.getItem('pin');
}

// used to check changes
let credorig = null;

// caches loaded data
let credshortlistdata = null;

// caches aliases
let aliases = null;

// control page navigation state
let state = '';

let filterTimer = null;

window.onpopstate = function (e) {
    let loc = new this.URL(document.location);

    console.log('onpostate loc:[' + loc + '] hash:[' + loc.hash + '] state:[' + state + ']');

    if (state == 'edit' && loc.hash == '') {
        if (!tryDiscardEdit()) {
            this.history.pushState(null, 'edit', '#edit');
        }
    }
}

// handle password clipboard
new ClipboardJS('.js-pwd-clip');

//----------------
// LOAD CRED LIST
//----------------
function reloadCredShortList(filter = '') {
    showPart('.js-main');
    $.post(urlbase + '/Api/CredShortList',
        {
            password: pwd,
            pin: pin,
            filter: filter
        },
        function (data, status, jqXHR) {
            if (checkApiError(data)) return;
            if (checkApiSuccessful(data)) {
                credshortlistdata = data.credShortList;

                let html = '<table class="table table-striped">';
                html += '<thead><tr>';
                html += '<th scope="col">Service</th>';
                html += '<th scope="col">Username</th>';
                html += '<th scope="col">Email</th>';
                html += '</tr></thead>';
                html += '<tbody>';
                _.each(_.sortBy(credshortlistdata, (x) => x.name), (x) => {
                    html += '<tr>';
                    html += '<td><a href="#edit" onclick="openCred(\'' + x.guid + '\');">' + ((x.name == null) ? '' : x.name) + '</a></td>';
                    html += '<td><a href="#edit" onclick="openCred(\'' + x.guid + '\');">' + ((x.username == null) ? '' : x.username) + '</a></td>';
                    html += '<td><a href="#edit" onclick="openCred(\'' + x.guid + '\');">' + ((x.email == null) ? '' : x.email) + '</a></td>';
                    html += '</tr>';
                });
                html += '</tbody>';
                html += '</table>';
                $('#cred-short-list').html(html);
            }
            else {
                $.notify('invalid login', 'error');
                pin = '';
            }
        });
}

//--------------
// LOAD ALIASES
//--------------
function reloadAliases() {
    $.post(urlbase + '/Api/Aliases',
        {
            password: pwd,
            pin: pin
        },
        function (data, status, jqXHR) {
            if (checkApiError(data)) return;
            if (checkApiSuccessful(data)) {
                aliases = data.aliases;
            }
            else {
                $.notify('invalid login', 'error');
                pin = '';
            }
        });
}

//------------------
// CRED LIST FILTER 
//------------------
function doFilter() {
    if (filterTimer != null) clearTimeout(filterTimer);

    var ctl = $('#credshortlistfilter');
    var filter = ctl[0].value;
    if (filter.length > 0) {
        ctl.addClass('filter-active');
        filterTimer = setTimeout(function () {
            reloadCredShortList(filter);
        }, 250);
    }
    else {
        reloadCredShortList(filter);
        ctl.removeClass('filter-active');
    }
}

$('#credshortlistfilter').keyup(function (e) {
    doFilter();
});

//----------------------------
// CRED LIST FILTER ( clear )
//----------------------------
$('#credshortlistfilterclear').click(function (e) {
    var ctl = $('#credshortlistfilter');
    ctl[0].value = '';
    ctl.removeClass('filter-active');
    ctl.focus();

    doFilter();
});

//-----------------------
// CREATE NEW CREDENTIAL
//-----------------------
$('.js-create-btn').click(function (e) {
    editExistingCred = false;

    //credorigname = '';

    $('#cred-guid')[0].value = '';
    $('#cred-name-box')[0].value = '';
    $('#cred-link-box')[0].value = '';
    $('#cred-username-box')[0].value = '';
    $('#cred-email-box')[0].value = '';
    $('#cred-pass-box')[0].value = '';
    $('#cred-notes-box')[0].value = '';

    gotoState('edit');
});

//-----------------
// EDIT CREDENTIAL
//-----------------
function openCred(e) {
    let guid = e;

    $.post(urlbase + '/Api/LoadCred',
        {
            password: pwd,
            pin: pin,
            guid: guid
        },
        function (data, status, jqXHR) {
            if (checkApiError(data)) return;
            if (checkApiSuccessful(data)) {
                $('#cred-guid')[0].value = data.cred.guid;
                $('#cred-name-box')[0].value = data.cred.name;
                $('#cred-link-box')[0].value = data.cred.url;
                $('#cred-username-box')[0].value = data.cred.username;
                $('#cred-email-box')[0].value = data.cred.email;
                $('#cred-pass-box')[0].value = data.cred.password;
                $('#cred-notes-box')[0].value = data.cred.notes;                            
                $('#cred-create-timestamp')[0].value = (data.cred.createTimestamp != null) ? moment(data.cred.createTimestamp).format('l LT') : "";
                $('#cred-modify-timestamp')[0].value = (data.cred.modifyTimestamp != null) ? moment(data.cred.modifyTimestamp).format('l LT') : "";

                credorig = JSON.stringify(buildCredObj());

                gotoState('edit');
            }
            else {
                $.notify('invalid login', 'error');
                pin = '';
            }                    
        });

    return false;
}

//----------------
// SAVE CRED EDIT
//----------------
$('.js-cred-save-btn').click(function (e) {
    if (isEmptyCredObj()) {
        $.notify('cannot save empty', 'warning');
        return;
    }

    $.post(
        urlbase + '/Api/SaveCred',
        {
            password: pwd, pin: pin,
            cred: buildCredObj()
        },
        function (data, status, jqXHR) {
            if (checkApiError(data)) return;
            if (checkApiInvalidAuth(data)) showPart('.js-login');
            else {
                aliases.push( {
                    name: $('#cred-name-box')[0].value,
                    username: $('#cred-username-box')[0].value,
                    email: $('#cred-email-box')[0].value
                });
                $.notify('data saved', 'success');
                doFilter();
            }
        }
    );
});

//-------------------
// DISCARD CRED EDIT
//-------------------
$('.js-cred-discardbtn').click(function (e) {
    if (tryDiscardEdit()) {
        history.pushState(null, '', '/');
    }
});

//--------------------------
// CRED EDIT REGEN PASSWORD
//--------------------------
$('#pwd-regen-btn').click(function (e) {
    let ctl = $('#cred-pass-box');

    ctl[0].value = 'generating...';

    $.post(
        urlbase + '/Api/RandomPassword',
        { password: pwd, pin: pin },
        function (data, status, jqXHR) {
            if (checkApiError(data)) return;
            if (checkApiInvalidAuth(data)) showPart('.js-login');
            else ctl[0].value = data.password;
        }
    );
});

//------------------
// CRED EDIT DELETE
//------------------
$('.js-cred-delete-btn').click(function (e) {
    if (confirm('sure to delete ?')) {
        $.post(
            urlbase + '/Api/DeleteCred',
            {
                password: pwd, pin: pin,
                guid: $('#cred-guid')[0].value
            },
            function (data, status, jqXHR) {
                if (checkApiError(data)) return;
                if (checkApiInvalidAuth(data)) showPart('.js-login');
                else {
                    $.notify('data saved', 'success');
                    reloadCredShortList();
                }
            }
        );
    }
});

// check if login required
$.post(
    urlbase + '/Api/IsAuthValid',
    { password: pwd, pin: pin },
    function (data, status, jqXHR) {
        if (checkApiError(data)) return;
        if (checkApiInvalidAuth(data))
            gotoState('login');
        else {
            reloadAliases();
            reloadCredShortList();
        }
    }
);

// tryDiscardEdit
function tryDiscardEdit() {
    if (JSON.stringify(buildCredObj()) == credorig || confirm('Discard changes ?') == true) {
        gotoState('list');
        return true;
    }
    return false;
}