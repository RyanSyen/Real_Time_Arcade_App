// PURPOSE: Center-crop image to the width and height specified (upscale)
function crop(f, w, h, to = 'blob', type = 'images/jpeg') {
    return new Promise((resolve, reject) => {
        const img = document.createElement('img');

        img.onload = e => {
            URL.revokeObjectURL(img.src);

            // Resize algorithm ---------------------------
            let ratio = w / h;

            let nw = img.naturalWidth;
            let nh = img.naturalHeight;
            let nratio = nw / nh;

            let sx, sy, sw, sh;

            if (ratio >= nratio) {
                // Retain width, calculate height
                sw = nw;
                sh = nw / ratio;
                sx = 0;
                sy = (nh - sh) / 2;
            }
            else {
                // Retain height, calculate width
                sw = nh * ratio;
                sh = nh;
                sx = (nw - sw) / 2;
                sy = 0;
            }
            // --------------------------------------------

            const can = document.createElement('canvas');
            can.width = w;
            can.height = h;
            can.getContext('2d').drawImage(img, sx, sy, sw, sh, 0, 0, w, h);

            // Resolve to blob or dataURL
            if (to == 'blob') {
                can.toBlob(blob => resolve(blob), type);
            }
            else if (to == 'dataURL') {
                let dataURL = can.toDataURL(type);
                resolve(dataURL);
            }
            else {
                reject('ERROR: Specify blob or dataURL');
            }
        };

        img.onerror = e => {
            URL.revokeObjectURL(img.src);
            reject('ERROR: File is not an image');
        };

        img.src = URL.createObjectURL(f);
    });
}

// PURPOSE: Best-fit image within the width and height specified (no upscale)
function fit(f, w, h, to = 'blob', type = 'image/jpeg') {
    return new Promise((resolve, reject) => {
        const img = document.createElement('img');

        img.onload = e => {
            URL.revokeObjectURL(img.src);

            // Resize algorithm ---------------------------
            let ratio = w / h;

            let nw = img.naturalWidth;
            let nh = img.naturalHeight;
            let nratio = nw / nh;

            if (nw <= w && nh <= h) {
                // Smaller than targetted width and height, do nothing
                w = nw;
                h = nh;
            }
            else {
                if (nratio >= ratio) {
                    // Retain width, calculate height
                    h = w / nratio;
                }
                else {
                    // Retain height, calculate width
                    w = h * nratio;
                }
            }
            // --------------------------------------------

            const can = document.createElement('canvas');
            can.width = w;
            can.height = h;
            can.getContext('2d').drawImage(img, 0, 0, w, h);

            // Resolve
            if (to == 'blob') {
                can.toBlob(blob => resolve(blob), type);
            }
            else if (to == 'dataURL') {
                let dataURL = can.toDataURL(type);
                resolve(dataURL);
            }
            else {
                reject('ERROR: Specify blob or dataURL');
            }
        };

        img.onerror = e => {
            URL.revokeObjectURL(img.src);
            reject('ERROR: File is not an image');
        };

        img.src = URL.createObjectURL(f);
    });
}


///////////////////////////////////////////////////////
$(this.document).ready(function () {

    //Center the "info" bubble in the  "circle" div
    var divTop = ($("#divCircle").height() - $("#middleBubble").height()) / 2;
    var divLeft = ($("#divCircle").width() - $("#middleBubble").width()) / 2;
    $("#middleBubble").css("top", divTop + "px");
    $("#middleBubble").css("left", divLeft + "px");

    //Arrange the icons in a circle centered in the div
    numItems = $("#divCircle img").length; //How many items are in the circle?
    start = 0.0; //the angle to put the first image at. a number between 0 and 2pi
    step = (4 * Math.PI) / numItems; //calculate the amount of space to put between the items.

    //Now loop through the buttons and position them in a circle
    $("#divCircle img").each(function (index) {
        radius = ($("#divCircle").width() - $(this).width()) / 2.3; //The radius is the distance from the center of the div to the middle of an icon
        //the following lines are a standard formula for calculating points on a circle. x = cx + r * cos(a); y = cy + r * sin(a)
        //We have made adjustments because the center of the circle is not at (0,0), but rather the top/left coordinates for the center of the div
        //We also adjust for the fact that we need to know the coordinates for the top-left corner of the image, not for the center of the image.
        tmpTop = (($("#divCircle").height() / 2) + radius * Math.sin(start)) - ($(this).height() / 2);
        tmpLeft = (($("#divCircle").width() / 2) + radius * Math.cos(start)) - ($(this).width() / 2);
        start += step; //add the "step" number of radians to jump to the next icon

        //set the top/left settings for the image
        $(this).css("top", tmpTop);
        $(this).css("left", tmpLeft);
    });

});

$('.avatarList').click(function () {
    $(this).toggleClass('expand');
    $('#divCircle').toggleClass('expand');
});

$('#divCircle img').click(function () {
    var theSrc = $(this).attr('src');
    // alert(theSrc);
    $('.mainImg img').attr('src', theSrc);
});
