import React, { useEffect, useRef } from 'react';
export const IFrame = ({ children, ...props }) => {
    const contentRef = useRef(null);
    const mountNode = contentRef?.current?.contentWindow?.document?.body;
    useEffect(() => {
        if (contentRef.current !== null) {
            contentRef.current.setAttribute('sandbox', 'allow-scripts');
        }
    }, [contentRef]);
    useEffect(() => {
        const htmlString = `<!DOCTYPE html>
    <html>
    <body>
      <script type="module" src="/auth-client.mjs"></script>
    </body>
    </html>`;
        // Convert children to HTML string
        // const htmlContent = children?.toString() ?? ''
        const blob = new Blob([htmlString], { type: 'text/html' });
        const url = URL.createObjectURL(blob);
        if (contentRef?.current !== null) {
            contentRef.current.src = url;
        }
        // Clean up the object URL on unmount
        return () => {
            URL.revokeObjectURL(url);
        };
    }, [contentRef]);
    return (React.createElement("iframe", { ...props, ref: contentRef }));
};
