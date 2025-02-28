import { initializeApp } from 'https://www.gstatic.com/firebasejs/10.14.1/firebase-app.js'
import { getAuth, signInWithEmailAndPassword, GoogleAuthProvider, signInWithPopup } from 'https://www.gstatic.com/firebasejs/10.14.1/firebase-auth.js'

async function signInWith(output, config, signInMethod){
    const app = initializeApp(config);
    const auth = getAuth(app);

    try {
        const info = await signInMethod(auth);
        await output.invokeMethodAsync("AfterSignIn", true, info, null)
    } catch(error) {
        await output.invokeMethodAsync("AfterSignIn", false, null, error.toString())
    }
}

export function storeAfterSignIn(encoded){
    document.cookie = `after-signin=${encoded}; path=/; max-age=30; Secure; SameSite=Strict`;
}

export function signinPassword(output, config, email, password) {
    return signInWith(output, config, async (auth) => {
        const result = await signInWithEmailAndPassword(auth, email, password);
        const user = result.user;
        const info = {
            type: "password",
            accessToken: user.accessToken,
            refToken: null   // Firebase password uses OAuth so there is no ID token
        }
        if (!!user.stsTokenManager) {
            info.refresh = {
                token: user.stsTokenManager.refreshToken,
                expires: user.stsTokenManager.expirationTime
            }
        }
        return info;
    })
}

export async function signin(output, config) {
    return signInWith(output, config, async (auth) => {
        const provider = new GoogleAuthProvider();
        const result = await signInWithPopup(auth, provider);
        const credential = GoogleAuthProvider.credentialFromResult(result);
        const user = result.user;
        const info = {
            type: "google",
            accessToken: user.accessToken,
            refToken: credential.accessToken
        }
        if (!!user.stsTokenManager) {
            info.refresh = {
                token: user.stsTokenManager.refreshToken,
                expires: user.stsTokenManager.expirationTime
            }
        }
        return info;
    })
}

let internalTimerId = null

export async function installExpirationTimer(expiration, notifyThreshold) {
    const infoPanel = document.getElementById("timeout-alert");
    const infoText = document.getElementById("timeout-alert-text");
    if (!infoPanel || !infoText) return;

    const time = new Date(expiration);
    const now = new Date();
    const diffMilliseconds = time - now

    const scheduleShowTimeLeft = () => {
        clearInterval(internalTimerId);

        internalTimerId = setInterval(() => {
            infoPanel.style.display = "initial";

            const now = new Date();
            const diffSeconds = (time - now) / 1000;

            if (diffSeconds > 3600)
                infoText.innerText = `${Math.round(diffSeconds / 3600)} ชั่วโมง`;
            else if (diffSeconds > 60)
                infoText.innerText = `${Math.round(diffSeconds / 60)} นาที`;
            else if (diffSeconds > 0)
                infoText.innerText = `${Math.round(diffSeconds)} วินาที`;
            else {
                infoText.innerText = "ตอนนี้";
                document.location.reload()  // refresh page and let the server handle the expiration check
            }
        }, 1000);
    }

    if (diffMilliseconds > notifyThreshold){
        const toNotify = diffMilliseconds - notifyThreshold
        setTimeout(() => scheduleShowTimeLeft(), toNotify)
    }
    else
        scheduleShowTimeLeft()
}