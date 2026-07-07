// Rendert den Authenticator-QR-Code aus dem otpauth-URI (EnableAuthenticator-Seite).
(function () {
    var data = document.getElementById("qrCodeData");
    var target = document.getElementById("qrCode");
    if (!data || !target || typeof QRCode === "undefined") return;
    target.innerHTML = "";
    new QRCode(target, {
        text: data.getAttribute("data-url"),
        width: 190,
        height: 190,
        colorDark: "#181614",
        colorLight: "#ffffff",
        correctLevel: QRCode.CorrectLevel.M
    });
})();
