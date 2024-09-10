/// <reference types="react" />
export interface EmailCodeProps {
    onEmailCodeSubmit: (emailCode: string) => Promise<void>;
    sendEmailCode: () => Promise<void>;
}
declare const EmailCode: ({ onEmailCodeSubmit, sendEmailCode }: EmailCodeProps) => JSX.Element;
export default EmailCode;
