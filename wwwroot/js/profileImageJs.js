
let image = document.querySelector(".profile-section-img");
let file = document.querySelector(".profile-file");

file.addEventListener('change', function (event) {
    var file = event.target.files[0];

    if (file) {
        var reader = new FileReader();

        reader.onload = function (e) {
            // Set the src attribute of the image element to the data URL obtained from FileReader
            image.style.backgroundImage = 'url(' + e.target.result + ')';
        };

        // Read the contents of the selected file as a data URL
        reader.readAsDataURL(file);
    }
});