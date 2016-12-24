import krpc
conn = krpc.connect()
vessel = conn.space_center.active_vessel

root = vessel.parts.root
stack = [(root, 0)]
while len(stack) > 0:
    part, depth = stack.pop()
    print(' '*depth, part.title)
    for child in part.children:
        stack.append((child, depth+1))
