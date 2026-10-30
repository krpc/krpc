import krpc

conn = krpc.connect()
try:
    conn.space_center.load("missing")
except ValueError as error:
    print("Failed to load the save:", error)
