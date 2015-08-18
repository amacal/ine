var system = require('system');
var web = require('webpage');
var fs = require('fs');
var page = web.create();

page.settings.loadImages = false;
page.settings.userAgent = 'Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/44.0.2403.130 Safari/537.36';

function debug(message) {
    console.log('debug: ' + message);
}

function fatal(message) {
    console.log('debug: ' + message);
}

function render() {
    fs.write('dump.txt', page.content, 'w');
    page.render('dump.png');
}

function onError(msg, trace) {
    fatal(msg);
    render();
}

function onRequest(requestData, networkRequest) {
    if (requestData.url.substring(0,32).match(/google/g) == null && requestData.url.substring(0,32).match(/nitroflare/g) == null) {
        request.abort();
    } else {
        debug(requestData.url);
    }
}

function requireFile() {
    debug('checking if file exists');
    var file = page.evaluate(function() {
        var element = document.querySelector('span[title]');
        if (element === undefined || element === null || element.length === 0) return undefined;
        return 'ok';
    });
    if (file === undefined || file === null) {
        debug('the requested file doesn\'t exist');
        console.log('file-status: file doesn\'t exist');
        phantom.exit(0);
    }
}

function printFileName() {
    debug('getting file-name');
    var filename = page.evaluate(function() {
        return document.querySelector('span[title]').title;
    });
    console.log('file-name: ' + filename);
}

function printFileSize() {
    debug('getting file-size');
    var filesize = page.evaluate(function () {
        return document.querySelector('span[title] span').innerText;
    });
    console.log('file-size: ' + filesize);
}

function printFileStatus() {
    debug('getting file-status');
    var message = page.evaluate(function () {
        var element = document.getElementById('error');
        if (element === undefined || element === null) return undefined;
        return element.innerText;
    });
    if (message !== undefined && message !== null) {
        debug('the requested file reported status as: ' + message);
        console.log('file-status: ' + message);
        phantom.exit(0);
    }
}

function printCaptchaUrl() {
    debug('getting captcha-url');
    var url = page.evaluate(function() {
        var image = document.getElementById('recaptcha_challenge_image');
        if (image !== undefined && image !== null) return image.src;
        var audio = document.getElementById('recaptcha_audio_download');
        if (audio !== undefined && audio !== null) return audio.href;
        return null;
    });
    if (url !== undefined && url !== null && url.length > 0) {
        debug('found captcha-url: ' + url);
        console.log('captcha-url: ' + url);
    } else {
        debug('captcha-url not found; rendering and terminating');
        render();
        phantom.exit(0);
    }
}

function printDownloadUrl(onRetry) {
    debug('getting download-url');
    var url = page.evaluate(function () {
        var element = document.getElementById('download');
        if (element === undefined || element === null) return undefined;
        return element.href;
    });
    if (url !== undefined && url !== null && url.length > 0) {
        debug('found download-url: ' + url);
        console.log('download-url: ' + url);
        phantom.exit(0);
    } else {
        debug('download-url not found; retrying');
        onRetry();
    }
}

function printErrorMessage() {
    debug('getting error-message');
    var message = page.evaluate(function () {
        var element = document.querySelector('.errMsg');
        if (element === undefined || element === null) return undefined;
        return element.innerText;
    });
    if (message !== undefined && message !== null && message.length > 0) {
        debug('found error-message: ' + message);
        console.log('message: ' + message);
        phantom.exit(0);
    }
}

function clickSlowDownload() {
    debug('clicking slow-download');
    var result = page.evaluate(function () {
        var element = document.getElementById('slow-download');
        if (element === undefined || element === null) return undefined;
        element.click();
        return true;
    });
    if (result === undefined || result === null) {
        debug('slow-download button not found; terminating');
        render();
        phantom.exit(0);
    }
}

function clickStartTimer() {
    debug('clicking start-timer');
    var result = page.evaluate(function () {
        var element = document.getElementById('beforeStartTimerBtn');
        if (element === undefined || element === null) return undefined;
        element.click();
        return true;
    });
    if (result === undefined || result === null) {
        debug('start-timer button not found; terminating');
        render();
        phantom.exit(0);
    }
}

function sendCaptcha(onContinue) {
    debug('waiting for captcha');
    var solution = system.stdin.readLine();
    if (solution === '::reload::') {
        debug('reloading captcha');
        page.evaluate(function () {
            document.getElementById('recaptcha_reload').click();
        });
        setTimeout(function () {
            handleCaptcha(onContinue);
        }, 3000);
    } else if (solution === '::audio::') {
        debug('switching captcha to audio');
        page.evaluate(function () {
            document.getElementById('recaptcha_switch_audio').click();
        });
        setTimeout(function () {
            handleCaptcha(onContinue);
        }, 3000);
    } else if (solution === '::image::') {
        debug('switching captcha to image');
        page.evaluate(function () {
            document.getElementById('recaptcha_switch_img').click();
        });
        setTimeout(function () {
            handleCaptcha(onContinue);
        }, 3000);
    } else {
        debug('sending captcha');
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
    page.open(url, function() {
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
    page.open('http://nitroflare.com', function() {
        page.open(url, function() {
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
    });
};

setTimeout(function() {
    phantom.exit(0);
}, 900000);

function normalize(url) {
    return url.replace('http://www.nitroflare.com/', 'http://nitroflare.com/');
}

if (system.args[1] === 'query') {
    query(normalize(system.args[2]));
}

else if (system.args[1] === 'download') {
    download(normalize(system.args[2]));
}

else {
    phantom.exit(0);
}