# [docs]
import krpc

conn = krpc.connect()
site = conn.eva.landing_site
print(site.name, site.latitude)
# [/docs]
