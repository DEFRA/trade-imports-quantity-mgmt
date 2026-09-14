#!/bin/bash
set -e

# Create test topic to mimic the data api
awslocal sns create-topic --name trade_imports_data_upserted
	
# SQS queues
awslocal sqs create-queue --queue-name trade_imports_data_upserted_quantity_mgmt-deadletter
awslocal sqs create-queue --queue-name trade_imports_data_upserted_quantity_mgmt --attributes '{"RedrivePolicy": "{\"deadLetterTargetArn\":\"arn:aws:sqs:eu-west-2:000000000000:trade_imports_data_upserted_quantity_mgmt-deadletter\",\"maxReceiveCount\":\"1\"}"}'

awslocal sqs create-queue --queue-name trade_imports_data_upserted_quantity_mgmt-test --attributes '{"RedrivePolicy": "{\"deadLetterTargetArn\":\"arn:aws:sqs:eu-west-2:000000000000:trade_imports_data_upserted_quantity_mgmt-deadletter\",\"maxReceiveCount\":\"1\"}"}'

# create the SNS subscription for the queue
awslocal sns subscribe --topic-arn arn:aws:sns:$AWS_REGION:000000000000:trade_imports_data_upserted --protocol sqs --notification-endpoint arn:aws:sqs:$AWS_REGION:000000000000:trade_imports_data_upserted_quantity_mgmt --attributes '{"RawMessageDelivery":"true"}'
awslocal sns subscribe --topic-arn arn:aws:sns:$AWS_REGION:000000000000:trade_imports_data_upserted --protocol sqs --notification-endpoint arn:aws:sqs:$AWS_REGION:000000000000:trade_imports_data_upserted_quantity_mgmt-test --attributes '{"RawMessageDelivery":"true"}'
