# InkyDesk Client

The Python script does all of the work. The shell script just sets the environment and calls the Python script.

## Installing and Running

I installed this on a Raspberry Pi Zero 2, but it should work on just about any modern-ish Pi running Raspberry Pi OS. I used the "Lite" version with SSH enabled.

- Follow the [instructions for the inky library](http://github.com/pimoroni/inky)
- Install git if you don't have it.
- Clone this repo to the Pi.
- Add the following lines to cron using `crontab -e`:

```crontab
@hourly /home/inkydesk/inky-desk/client/inkydesk.sh
@reboot /home/inkydesk/inky-desk/client/inkydesk.sh
```

Update paths as needed to match your setup.
