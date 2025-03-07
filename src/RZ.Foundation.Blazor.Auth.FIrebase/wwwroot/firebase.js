import { initializeApp } from 'https://www.gstatic.com/firebasejs/10.14.1/firebase-app.js'
import { getAuth, createUserWithEmailAndPassword, signInWithEmailAndPassword,
    FacebookAuthProvider,
    GoogleAuthProvider,
    signInWithCustomToken,
    sendEmailVerification, sendPasswordResetEmail,
    signInWithPopup } from 'https://www.gstatic.com/firebasejs/10.14.1/firebase-auth.js'

/**
 * Get Firebase Authentication instance
 * @param {Object} config - Firebase configuration object
 * @returns {Object} - Firebase Authentication instance
 */
function getFbAuth(config){
    const app = initializeApp(config);
    return getAuth(app);
}

/**
 * Sign in with a Firebase authentication method, then notify the Blazor app with `AfterSignIn` method.
 * @param {Object} output - Blazor app's JS interop object
 * @param {Object} config - Firebase configuration object
 * @param {Function} signInMethod - Firebase sign-in method (e.g., signInWithEmailAndPassword)
 * @returns {Promise<void>}
 */
async function signInWith(output, config, signInMethod){
    const auth = getFbAuth(config);

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

export function signInPassword(output, config, email, password) {
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

export function signInWithProvider(output, config, providerType, type) {
    return signInWith(output, config, async (auth) => {
        const provider = new providerType();
        const result = await signInWithPopup(auth, provider);
        const credential = providerType.credentialFromResult(result);
        const user = result.user;
        const info = {
            type,
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

export function signInGoogle(output, config) {
    return signInWithProvider(output, config, GoogleAuthProvider, "google");
}

export function signInFacebook(output, config) {
    return signInWithProvider(output, config, FacebookAuthProvider, "facebook");
}

export function signUpPassword(output, config, email, password){
    return signInWith(output, config, async (auth) => {
        const result = await createUserWithEmailAndPassword(auth, email, password);
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

export function signInWithJwt(output, config, type, token){
    return signInWith(output, config, async (auth) => {
        const result = await signInWithCustomToken(auth, token);
        console.log(result)
        const user = result.user;
        const info = {
            type,
            accessToken: user.accessToken,
            refToken: token
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

export async function verifyEmail(config, email){
    const auth = getFbAuth(config);
    await sendEmailVerification(auth.currentUser);
}

export async function resetPassword(config, email){
    const auth = getFbAuth(config);
    try{
        await sendPasswordResetEmail(auth, email);
        return null;
    }
    catch(error){
        return error.toString();
    }
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