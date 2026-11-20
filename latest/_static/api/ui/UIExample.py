import krpc

conn = krpc.connect(name="User Interface Example")
conn.ui.message("Hello, World!", duration=10)
