import React from 'react';
import './index.css';
import { RouterProvider, createBrowserRouter } from 'react-router-dom';
import Signin from './views/Signin';
import { Toaster } from 'react-hot-toast';
import { createRoot } from 'react-dom/client';
import EmailCode from './views/EmailCode';
import './global.css';
export const routes = {
    signin: '/',
    signup: '/signup',
    emailCode: '/email-code'
};
const router = createBrowserRouter([
    {
        path: routes.signin,
        element: React.createElement(Signin, null)
    },
    {
        path: routes.emailCode,
        element: React.createElement(EmailCode, null)
    }
]);
const container = document.getElementById('root');
if (!container)
    throw new Error('No root element');
const root = createRoot(container); // createRoot(container!) if you use TypeScript
root.render(React.createElement(React.StrictMode, null,
    React.createElement(RouterProvider, { router: router }),
    React.createElement(Toaster, null)));
