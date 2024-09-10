import React, { useEffect } from 'react';
import IframeCommunicationRelay from '../util/iframe';
const relay = new IframeCommunicationRelay();
const SigningCore = () => {
    useEffect(() => {
        console.log('SigningCore!!!');
        relay.defineJSONRPCMethod('addNumbers', async (num1, num2) => {
            console.log('addNumbers', num1, num2);
            return num1 + num2;
        });
        // return () => {
        //   relay.unsubscribeAll('addNumbers')
        // }
    }, []);
    return React.createElement("div", null);
};
export default SigningCore;
