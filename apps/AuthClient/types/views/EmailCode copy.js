import React, { useState, useRef, useEffect } from 'react';
import { Spinner } from '../components';
import { argon } from '../lib';
import toast from 'react-hot-toast';
import useOAuthParams from '../hooks/oauthParams';
const EmailCode = () => {
    const [code, setCode] = useState(new Array(6).fill(''));
    const inputsRef = useRef(new Array(6).fill(null));
    const [isLoading, setIsLoading] = useState(false);
    const { redirectUri, state, codeChallenge, username, cspk } = useOAuthParams();
    const emailSentRef = useRef(false);
    useEffect(() => {
        if (username !== undefined && !emailSentRef.current) {
            emailSentRef.current = true; // Mark that an email is being sent.
            argon.initRegister({
                username,
                redirectUri,
                state,
                codeChallenge,
                cspk
            }, username)
                .then(() => {
                toast.success('Email Code sent to your email.');
            })
                .catch(e => {
                toast.error('Email Code failed to send. Try Again.');
                console.error(e);
            });
        }
        // Since we don't want useEffect to re-run when `emailSentRef.current` changes, we leave it out of the dependency array.
        // This useEffect is only concerned with `username` changing.
    }, [username]); // Only re-run the effect if `username` changes
    const handleInputChange = (event, index) => {
        const value = event.target.value;
        setCode(prevCode => {
            const newCode = [...prevCode];
            newCode[index] = value;
            return newCode;
        });
        if (value !== undefined && index < 5) {
            inputsRef.current[index + 1]?.focus();
        }
    };
    const handleOnPaste = (event) => {
        event.preventDefault();
        const pasteData = event.clipboardData.getData('text').slice(0, 6);
        setCode(pasteData.split(''));
        const nextInputIndex = inputsRef.current.findIndex((input, idx) => input?.value === '' && idx >= pasteData.length);
        if (nextInputIndex !== -1) {
            inputsRef.current[nextInputIndex]?.focus();
        }
    };
    const handleVerifyWork = async () => {
        setIsLoading(true);
        const emailCode = code.join('');
        try {
            await argon.getEmailCodeIsValid(username, emailCode);
            toast.success('Code is valid');
            await argon.register({
                username,
                redirectUri,
                state,
                codeChallenge,
                cspk
            }, emailCode);
            toast.success('Sign in successful');
        }
        catch (e) {
            toast.error('Either the email code is invalid or registration failed. Please try again.');
            console.error(e);
        }
        finally {
            setIsLoading(false);
        }
    };
    const handleVerify = () => {
        handleVerifyWork().then(() => {
            console.log('Verify successful');
        }).catch(e => {
            console.error(e);
        });
    };
    return (React.createElement("main", { className: "h-screen flex flex-col justify-center items-center fixed left-0 top-0 right-0 bottom-0" },
        React.createElement("div", { className: "mx-auto max-w-lg text-center flex items-center space-x-2 my-4" },
            React.createElement("h1", { className: "text-2xl font-bold sm:text-3xl" }, "Email Code"),
            React.createElement("svg", { xmlns: "http://www.w3.org/2000/svg", fill: "none", viewBox: "0 0 24 24", strokeWidth: 1.5, stroke: "currentColor", className: "w-12 h-12 text-orange-300" },
                React.createElement("path", { strokeLinecap: "round", strokeLinejoin: "round", d: "M9.813 15.904L9 18.75l-.813-2.846a4.5 4.5 0 00-3.09-3.09L2.25 12l2.846-.813a4.5 4.5 0 003.09-3.09L9 5.25l.813 2.846a4.5 4.5 0 003.09 3.09L15.75 12l-2.846.813a4.5 4.5 0 00-3.09 3.09zM18.259 8.715L18 9.75l-.259-1.035a3.375 3.375 0 00-2.455-2.456L14.25 6l1.036-.259a3.375 3.375 0 002.455-2.456L18 2.25l.259 1.035a3.375 3.375 0 002.456 2.456L21.75 6l-1.035.259a3.375 3.375 0 00-2.456 2.456zM16.894 20.567L16.5 21.75l-.394-1.183a2.25 2.25 0 00-1.423-1.423L13.5 18.75l1.183-.394a2.25 2.25 0 001.423-1.423l.394-1.183.394 1.183a2.25 2.25 0 001.423 1.423l1.183.394-1.183.394a2.25 2.25 0 00-1.423 1.423z" }))),
        React.createElement("p", { className: "text-gray-500 text-center mb-12" }, "Enter the code we sent to your email address."),
        React.createElement("div", { className: "flex space-x-4" }, code.map((num, index) => (React.createElement("input", { key: index, ref: (el) => {
                inputsRef.current[index] = el;
            }, onPaste: handleOnPaste, type: "tel", maxLength: 1, value: num, onChange: (event) => {
                handleInputChange(event, index);
            }, className: "w-12 h-12 rounded-lg border-2 border-gray-300 text-center text-xl font-medium focus:border-blue-500 focus:outline-none", pattern: "\\d*", inputMode: "numeric", autoFocus: index === 0 })))),
        React.createElement("button", { onClick: handleVerify, className: "inline-block rounded-lg bg-blue-500 px-5 py-3 my-5 text-sm font-medium text-white flex transition-all hover:bg-blue-600 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500" },
            isLoading && React.createElement(Spinner, null),
            "Verify"),
        React.createElement("p", { className: "text-gray-500 text-center mt-10" },
            "Didn't receive the code?",
            ' ')));
};
export default EmailCode;
