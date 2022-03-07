let urlParams = new URLSearchParams(window.location.search);
let groupId = urlParams.get('id');

if (!groupId) {
    alert('Invalid Group Id');
    window.location = "room.html";
}