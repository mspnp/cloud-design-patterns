"use strict";

var img = $('#coverImage');
var txt = $('#coverText');

function showCoverText() {
    // show the text instead of the image
    txt.show();
    img.hide();
}

function hideCoverText() {
    // show the image instead of the text, fading for effect 
    txt.fadeOut(1000);
    img.fadeIn(1000);
}

// attach events for image load and error
img.on('load', hideCoverText);
img.on('error', showCoverText);

// if the image doesn't load with the page, show the text instead
$(function () {
    if (!img[0].complete || img[0].naturalWidth === 0) {
        showCoverText();
    }
});
