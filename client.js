function submit() {
    let server = document.getElementsByName("server")[0].value;
    let port = document.getElementsByName("port")[0].value;
    let pwd = document.getElementsByName("pwd")[0].value;
    let psk = document.getElementsByName("psk")[0].value;

    if (port > 65535 || port < 10000) {
        let res = document.getElementById("res");
        res.style.color = "#FF5733";
        res.innerHTML = "端口号无效";
    }
    else {
        let wsc = new WebSocket(server);
        wsc.onopen = function () {
            console.log("socket opened");
        }
        wsc.onclose = function () {
            console.log("socket closed");
        }
        wsc.onmessage = function (e) {
            let response = JSON.parse(e.data);
            if (response.code == 1) {
                if (wsc.readyState == 1) {
                    let authcontent = CryptoJS.HmacSHA256(response.content, psk);
                    wsc.send(encrypt({ code: 2, content: authcontent.toString() }, psk));
                    console.log("got challenge msg.");
                }

            }
            if (response.code == 3) {
                if (wsc.readyState == 1) {
                    wsc.send(encrypt({ code: 4, server: { port: port, password: pwd } }, psk));
                    console.log("config sent");
                }

            }
            if (response.code == 0) {
                console.log(response.content);
                res.innerHTML = response.content;
            }
        }
    }

}
function encrypt(json, psk) {
    plaintext = CryptoJS.enc.Utf8.parse(JSON.stringify(json));
    let key = CryptoJS.enc.Utf8.parse(psk.padEnd(32, '0'));
    let iv = CryptoJS.enc.Utf8.parse(psk.padEnd(16, '0'));
    let encrypted = CryptoJS.AES.encrypt(plaintext, key, {
        iv: iv,
        mode: CryptoJS.mode.CBC,
        padding: CryptoJS.pad.Pkcs7
    });
    return CryptoJS.enc.Base64.stringify(encrypted.ciphertext);
}