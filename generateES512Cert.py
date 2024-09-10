import subprocess
import base64

def run_openssl_command(command):
    process = subprocess.run(command, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    if process.returncode != 0:
        raise Exception(f"Command '{' '.join(command)}' failed with error:\n{process.stderr.decode()}")
    return process.stdout

try:
    # Step 1: Generate the ES512 private key in DER format
    der_private_key_output = run_openssl_command(['openssl', 'ecparam', '-name', 'secp521r1', '-genkey', '-noout', '-outform', 'DER'])

    # Step 2: Encode the DER formatted private key to BASE64
    b64_encoded_der_private_key = base64.b64encode(der_private_key_output).decode('utf-8')

    print("BASE64 encoded ES512 private key (DER format):")
    print(b64_encoded_der_private_key)

except Exception as exc:
    print(f"An error occurred: {exc}")
