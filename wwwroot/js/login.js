// if user did not sign in, navigate back to index.html =================================
let username = sessionStorage.getItem('name');
let photo = sessionStorage.getItem('photo');

if (!username || !photo) {
    alert('You need to sign in first!');
    window.location = "/";
}