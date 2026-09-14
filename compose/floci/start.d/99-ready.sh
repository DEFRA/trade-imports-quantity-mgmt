#!/bin/bash

function is_ready() {

  awslocal sqs get-queue-url --queue-name trade_imports_data_upserted_quantity_mgmt || return 1
  return 0
}

is_ready