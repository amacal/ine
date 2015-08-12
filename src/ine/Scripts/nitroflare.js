var system = require('system');
var web = require('webpage');
var fs = require('fs');

var page = web.create();
page.settings.loadImages = false;

function onError(msg, trace) {
    console.log('fatal: ' + msg);
    fs.write('dump.txt', page.content, 'w');
    page.render('dump.png');
    phantom.exit(1);
}

function onRequest(requestData, networkRequest) {
    console.log('request: ' + requestData.url);
}

function requireFile() {
    var file = page.evaluate(function() {
        var element = document.querySelector('span[title]');
        if (element === undefined || element === null || element.length === 0) return undefined;
        return 'ok';
    });
    if (file === undefined || file === null) {
        console.log('file-status: file doesn\'t exist');
        phantom.exit(0);
    }
}

function printFileName() {
    var filename = page.evaluate(function() {
        return document.querySelector('span[title]').title;
    });
    console.log('file-name: ' + filename);
}

function printFileSize() {
    var filesize = page.evaluate(function() {
        return document.querySelector('span[title] span').innerText;
    });
    console.log('file-size: ' + filesize);
}

function printFileStatus() {
    var message = page.evaluate(function() {
        var element = document.getElementById('error');
        if (element === undefined || element === null) return undefined;
        return element.innerText;
    });
    if (message !== undefined && message !== null) {
        console.log('file-status: ' + message);
        phantom.exit(0);
    }
}

function printCaptchaUrl() {
    var url = page.evaluate(function() {
        return document.getElementById('recaptcha_challenge_image').src;
    });
    console.log('captcha-url: ' + url);
}

function printDownloadUrl() {
    var url = page.evaluate(function() {
        return document.getElementById('download').href;
    });
    console.log('download-url: ' + url);
}

function printErrorMessage() {
    var message = page.evaluate(function() {
        var element = document.querySelector('.errMsg');
        if (element === undefined || element === null) return undefined;
        return element.innerText;
    });
    if (message !== undefined && message !== null) {
        console.log('message: ' + message);
        phantom.exit(0);
    }
}

function clickSlowDownload() {
    page.evaluate(function() {
        document.getElementById('slow-download').click();
    });
}

function clickStartTimer() {
    page.evaluate(function() {
        document.getElementById('beforeStartTimerBtn').click();
    });
}

function sendCaptcha(onContinue) {
    var solution = system.stdin.readLine();
    if (solution.length !== 0) {
        page.evaluate(function(solution) {
            document.getElementById('recaptcha_response_field').value = solution;
            document.getElementById('sendReCaptcha').click();
        }, solution);
        onContinue();
    } else {
        page.evaluate(function() {
            document.getElementById('recaptcha_reload').click();
        });
        setTimeout(function() {
            handleCaptcha(onContinue);
        }, 3000);
    }
}

function handleCaptcha(onContinue) {
    printCaptchaUrl();
    sendCaptcha(onContinue);
}

function query(url) {
    phantom.onError = onError;
    page.onError = onError;
    page.onResourceRequested = onRequest;
    page.open(url, function(status) {
        requireFile();
        printFileName();
        printFileSize();
        printFileStatus();
        phantom.exit(0);
    });
}

function download(url) {
    phantom.onError = onError;
    page.onError = onError;
    page.onResourceRequested = onRequest;
    page.open(url, function(status) {
        requireFile();
        printFileName();
        printFileSize();
        printFileStatus();
        clickSlowDownload();
        setTimeout(function() {
            clickStartTimer();
            setTimeout(function() {
                handleCaptcha(function() {
                    setTimeout(function() {
                        printErrorMessage();
                        printDownloadUrl();
                        phantom.exit(0);
                    }, 10000);
                });
            }, 70000);
        }, 20000);
    });
};

setTimeout(function() {
    phantom.exit(0);
}, 900000);

if (system.args[1] === 'query') {
    query(system.args[2]);
}

else if (system.args[1] === 'download') {
    download(system.args[2]);
}

else {
    phantom.exit(0);
}