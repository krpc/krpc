import krpc

conn = krpc.connect()

# [docs]
stats = conn.krpc.get_status().statistics
print("Frame rate: %.0f fps" % stats.frame_rate)
print("Calls: %.0f a second" % stats.rpc_rate)
print(
    "Server per frame: %.1f of %.1f ms"
    % (stats.time_per_frame * 1e3, stats.budget_per_frame * 1e3)
)
print("Waiting per frame: %.1f ms" % (stats.wait_time_per_frame * 1e3))
print("Median turnaround: %.1f ms" % (stats.median_turnaround * 1e3))
# [/docs]
