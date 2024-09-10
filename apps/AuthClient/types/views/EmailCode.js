import React, { useState, useRef, useEffect } from 'react';
import { Spinner } from '../components';
import toast from 'react-hot-toast';
const EmailCode = ({ onEmailCodeSubmit, sendEmailCode }) => {
    const [code, setCode] = useState(new Array(6).fill(''));
    const inputsRef = useRef(new Array(6).fill(null));
    const [isLoading, setIsLoading] = useState(false);
    const emailSentRef = useRef(false);
    useEffect(() => {
        if (sendEmailCode !== undefined && !emailSentRef.current) {
            emailSentRef.current = true; // Mark that an email is being sent.
            sendEmailCode()
                .then(() => {
                toast.success('Email Code sent to your email.');
            })
                .catch((e) => {
                toast.error('Email Code failed to send. Try Again.');
                console.error(e);
            });
        }
    }, []);
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
            await onEmailCodeSubmit(emailCode);
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
    return (React.createElement("main", { style: {
            display: 'flex',
            flexDirection: 'column',
            justifyContent: 'center',
            alignItems: 'center',
            background: 'white',
            padding: '1rem' // Tailwind p-4 (1rem assuming 1rem=16px)
        } },
        React.createElement("div", { style: {
                margin: 'auto',
                maxWidth: '32rem',
                textAlign: 'center',
                display: 'flex',
                alignItems: 'center',
                columnGap: '1rem',
                marginBottom: '1rem'
            } },
            React.createElement("h1", { style: {
                    fontSize: '1.875rem',
                    fontWeight: 'bold',
                    color: '#000' // Tailwind text-gray-900, assuming not in dark mode
                } }, "Email Code")),
        React.createElement("p", { style: {
                color: '#718096',
                textAlign: 'center',
                marginBottom: '48px' // Tailwind mb-12 (3rem assuming 1rem=16px)
            } }, "Enter the code we sent to your email address."),
        React.createElement("div", { style: {
                display: 'flex',
                columnGap: '16px' // Tailwind space-x-4 (1rem assuming 1rem=16px)
            } }, code.map((num, index) => (React.createElement("input", { key: index, ref: (el) => { inputsRef.current[index] = el; }, onPaste: handleOnPaste, type: "tel", maxLength: 1, value: num, onChange: event => { handleInputChange(event, index); }, style: {
                width: '48px',
                height: '48px',
                borderRadius: '12px',
                border: '2px solid #d2d6dc',
                textAlign: 'center',
                fontSize: '1.25rem',
                fontWeight: 'medium' // Tailwind font-medium
                // Tailwind focus:border-blue-500 & focus:outline-none styles cannot be implemented inline, would need JS
            }, pattern: "\\d*", inputMode: "numeric", autoFocus: index === 0 })))),
        React.createElement("button", { onClick: handleVerify, style: {
                display: 'inline-flex',
                alignItems: 'center',
                justifyContent: 'center',
                borderRadius: '0.375rem',
                backgroundColor: '#6366f1',
                padding: '12px 20px',
                fontSize: '0.875rem',
                fontWeight: 'medium',
                color: 'white',
                marginTop: '20px' // Tailwind my-5 (1.25rem assuming 1rem=16px)
                // Tailwind hover:bg-blue-600, focus:outline-none, focus:ring-2, focus:ring-offset-2, focus:ring-blue-500 styles cannot be implemented
            } },
            isLoading && React.createElement(Spinner, null),
            " ",
            "Verify"),
        React.createElement("p", { style: {
                color: '#718096',
                textAlign: 'center',
                marginTop: '40px' // Tailwind mt-10 (2.5rem assuming 1rem=16px)
            } },
            "Didn't receive the code?",
            ' ')));
};
export default EmailCode;
