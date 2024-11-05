# Docker Local Log Decoder

We recently switched over to docker's local log driver which uses protobuf for serializing the log entries.
This super small decoder is part of a larger log analysis tool we use internally. It does not use Google's protobuf
library, parsing is done manually and by utilizing .NET Pipeline IO. Partial log entries are not parsed, yet. I never
stumbled over a log entry that was partial, so i did not feel like implementing that.

Usage is pretty simple. Currently you put a list of files as parameters and the log entries are decoded and written to
the standard output. This makes it easy in case of log file rolling, to create a combined log from a set of log files.

Example:
```
LocalLogDecoder.exe DockerApp.log
LocalLogDecoder.exe DockerApp.1.log DockerApp.2.log DockerApp.3.log
```