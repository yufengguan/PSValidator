# Seq Dashboard Queries for PS Validator Analytics

To visualize the usage analytics, create a new Dashboard in Seq and add the following charts using these SQL-like queries.

## 1. Validation Traffic (Over Time)
**Chart Type:** Timeseries
**Query:**
```sql
select count(*) as Validations
from stream
where EventType = 'ValidationCompleted'
group by time(1h)
```

## 2. Top Services (Pie Chart)
**Chart Type:** Pie Chart
**Query:**
```sql
select count(*) as Usage
from stream
where EventType = 'ValidationCompleted'
group by Service
limit 10
```

## 3. Success vs Failure Rate
**Chart Type:** Timeseries (or Bar)
**Query:**
```sql
select count(*) as Total
from stream
where EventType = 'ValidationCompleted'
group by IsValid, time(1d)
```

## 4. Daily Active Users (Estimated)
**Chart Type:** Timeseries
**Query:**
```sql
select count(distinct(RefUserId)) as ActiveUsers
from stream
where RefUserId is not null
group by time(1d)
```

## 5. Average Response Time
**Chart Type:** Value (Single Stat) or Timeseries
**Query:**
```sql
select mean(DurationMs) as AvgDurationMs
from stream
where EventType = 'ValidationCompleted'
```

## 6. External Helper Response Time
**Chart Type:** Timeseries
**Explanation:** Time spent waiting for the external endpoint (e.g. your application) to respond.
**Query:**
```sql
select mean(ExternalDurationMs) as AvgExternalDurationMs
from stream
where EventType = 'ValidationCompleted' and ExternalDurationMs > -1
group by time(1h)
```
