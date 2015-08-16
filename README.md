ine downloader
==============

ine is a windows application to download files from popular file hosting sites

release notes
=============

# 0.6
- [IMPROVEMENT] Already downloaded resource is not startable
- [IMPROVEMENT] Closing tha application takes no longer than 10 seconds

# 0.5
- [FEATURE] Support getting files from nitroflare folders
- [IMPROVEMENT] ETA no longer trims minutes
- [IMPROVEMENT] Escape key works as No in the Close window
- [IMPROVEMENT] Closing the application with running transfers stops them first

# 0.4
- [IMPROVEMENT] Closing application with ongoing transfer triggers confirmation
- [IMPROVEMENT] All logs are addionally persisted in the app-data directory

# 0.3
- [FEATURE] Links are also used as a source of text to parse
- [FEATURE] Captcha can be switched to audio and back
- [IMPROVEMENT] PhantomJS sends less requests for unwanted content
- [IMPROVEMENT] PhantomJS working directory is set to app-data
- [IMPROVEMENT] Pressing enter in the captcha text field triggers solving it

# 0.2
- [BUG] Captcha timeout could terminate the application
- [BUG] PhantomJS fatal not related to the process could terminate the process

# 0.1
- Initial release