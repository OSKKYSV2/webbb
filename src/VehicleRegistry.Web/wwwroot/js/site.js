// Reject an oversized file in the browser before the upload starts.
// The server checks the same limit again (VehiclesController.UploadPhoto).
document.querySelectorAll('input[type=file][data-max-bytes]').forEach(function (input) {
    input.addEventListener('change', function () {
        var file = input.files && input.files[0];
        var tooLarge = file && file.size > Number(input.dataset.maxBytes);
        input.setCustomValidity(tooLarge ? input.dataset.maxMessage : '');
        input.reportValidity();
    });
});
