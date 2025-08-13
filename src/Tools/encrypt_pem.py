from cryptography.hazmat.primitives import serialization
from cryptography.hazmat.backends import default_backend

def encrypt_pem():
	with open("../../keys/test_pvk_private_key.pem", "rb") as f:
		private_key = serialization.load_pem_private_key(
			f.read(),
			password=None,
			backend=default_backend()
		)

	new_password = "test_password".encode("ascii")
	encrypted_pem = private_key.private_bytes(
		encoding=serialization.Encoding.PEM,
		format=serialization.PrivateFormat.PKCS8,
		encryption_algorithm=serialization.BestAvailableEncryption(new_password)
	)

	with open("../../keys/test_pvk_private_key_encrypted.pem", "wb") as f:
		f.write(encrypted_pem)

def decrypt_pem():
	new_password = "test_password".encode("ascii")
	with open("../../keys/test_pvk_private_key_encrypted.pem", "rb") as f:
		private_key = serialization.load_pem_private_key(
			f.read(),
			password=new_password,
			backend=default_backend()
		)

	decrypted_pem = private_key.private_bytes(
		encoding=serialization.Encoding.PEM,
		format=serialization.PrivateFormat.PKCS8,
		encryption_algorithm=serialization.NoEncryption()
	)

	with open("../../keys/test_pvk_private_key.pem", "wb") as f:
		f.write(decrypted_pem)


decrypt_pem()