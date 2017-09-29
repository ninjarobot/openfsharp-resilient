

When everything goes right, you don't need the ecosystem

Logging - journald (like syslog, but supports stuctured logs)
Lifetime - systemd (like upstart, initctl, but supports cgroups)

If you deploy to bare metal, you'll need to work in this zone.

Hopefully you don't.  Docker, mesos, kubernetes, consul - all of these can keep your application running and they already play in this zone.

