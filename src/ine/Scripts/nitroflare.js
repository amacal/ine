var system = require('system');
var web = require('webpage');
var fs = require('fs');
var page = web.create();

page.settings.loadImages = false;
page.settings.userAgent = 'Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/44.0.2403.130 Safari/537.36';

function onError(msg, trace) {
    console.log('fatal: ' + msg);
    fs.write('dump.txt', page.content, 'w');
    page.render('dump.png');
}

function onRequest(requestData, networkRequest) {
    if (requestData.url.substring(0,32).match(/google/g) == null && requestData.url.substring(0,32).match(/nitroflare/g) == null) {
        request.abort();
    } else {
        console.log('request: ' + requestData.url);
    }
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
        var image = document.getElementById('recaptcha_challenge_image');
        if (image !== undefined && image !== null) return image.src;
        var audio = document.getElementById('recaptcha_audio_download');
        if (audio !== undefined && audio !== null) return audio.href;
        return null;
    });
    if (url !== undefined && url !== null) {
        console.log('captcha-url: ' + url);
    }
}

function printDownloadUrl(onMissing) {
    var url = page.evaluate(function() {
        var element = document.getElementById('download');
        if (element === undefined || element === null) return undefined;
        return element.href;
    });
    if (url !== undefined && url !== null) {
        console.log('download-url: ' + url);
        phantom.exit(0);
    } else {
        console.log('debug: retrying captcha.');
        onMissing();
    }
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
    console.log('debug: waiting for captcha');
    var solution = system.stdin.readLine();
    if (solution === '::reload::') {
        console.log('debug: reloading captcha');
        page.evaluate(function () {
            document.getElementById('recaptcha_reload').click();
        });
        setTimeout(function () {
            handleCaptcha(onContinue);
        }, 3000);
    } else if (solution === '::audio::') {
        console.log('debug: switching captcha to audio');
        page.evaluate(function () {
            document.getElementById('recaptcha_switch_audio').click();
        });
        setTimeout(function () {
            handleCaptcha(onContinue);
        }, 3000);
    } else if (solution === '::image::') {
        console.log('debug: switching captcha to image');
        page.evaluate(function () {
            document.getElementById('recaptcha_switch_img').click();
        });
        setTimeout(function () {
            handleCaptcha(onContinue);
        }, 3000);
    } else {
        console.log('debug: sending captcha');
        page.evaluate(function (solution) {
            document.getElementById('recaptcha_response_field').value = solution;
            document.getElementById('sendReCaptcha').click();
        }, solution);
        onContinue();
    }
}

function decaptcha() {
    handleCaptcha(function () {
        setTimeout(function () {
            handleDownload();
        }, 10000);
    });
}

function handleCaptcha(onContinue) {
    printCaptchaUrl();
    sendCaptcha(onContinue);
}

function handleDownload() {
    printErrorMessage();
    printDownloadUrl(decaptcha);
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
                decaptcha();
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