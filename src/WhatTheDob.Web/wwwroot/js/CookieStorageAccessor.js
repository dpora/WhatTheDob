window.CookieStorageAccessor = {
    setCookie: function (name, value, days, isSecure) {
        var expires = "";
        if (days) {
            var date = new Date();
            date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
            expires = "; expires=" + date.toUTCString();
        }

        var secure = isSecure ? "; Secure" : "";
        // Mirror server cookie flags: path + SameSite=Lax
        document.cookie = name + "=" + (value || "") + expires + "; path=/; SameSite=Lax" + secure;
    }
};
