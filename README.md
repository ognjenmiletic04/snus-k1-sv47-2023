# snus-k1-sv47-2023
# Concurrent Job Processing System

Repository for the first midterm (K1) of the SNUS course.

## Overview

Thread-safe, asynchronous job processing system based on a producer–consumer model with priority scheduling.

## Features

* Thread-safe priority queue
* Worker threads + async execution (`Task`)
* Priority-based processing (lower value = higher priority)
* Max queue size limit

## Job Types

* **Prime** – counts prime numbers (parallel, 1–8 threads)
* **IO** – simulates I/O delay, returns random value

## Reliability

* Idempotent jobs (same ID executes once)
* Retry mechanism (2 retries → then `ABORT`)
* Timeout: jobs fail if > 2s

## Events

* `JobCompleted`, `JobFailed`
* Async file logging:
  `[DateTime] [Status] JobId, Result`

## Reporting

* Generated every minute (LINQ)
* Stats per job type + failures
* Stored as XML (last 10 reports kept)

## API

* `GetTopJobs(int n)`
* `GetJob(Guid id)`

## Configuration

XML-based:

* Worker count
* Initial jobs
* Max queue size

