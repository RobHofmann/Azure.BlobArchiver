#!/bin/bash

# Make sure CRON is being setup
if [[ -n "$CRON_EXPRESSION" ]]; then
  ln -sf /proc/$$/fd/1 /var/log/stdout
  service cron start
	if [[ -n "$CRON_EXPRESSION" ]]; then
        env >> /etc/environment
        echo "$CRON_EXPRESSION /app/Azure.BlobArchiver >/var/log/stdout 2>&1" > /etc/crontab
	fi
	crontab /etc/crontab
fi

# Tail to let the container run
tail -f /dev/null