import boto3
import re
import json

def lambda_handler(event, context):
    # Get the bucket and object key from the event
    bucket = event['Records'][0]['s3']['bucket']['name']
    key = event['Records'][0]['s3']['object']['key']

    # Create an Amazon Rekognition client
    rekognition = boto3.client('rekognition')

    # Call DetectText API to extract text from the image
    response = rekognition.detect_text(
        Image={
            'S3Object': {
                'Bucket': bucket,
                'Name': key
            }
        }
    )

    # Extract the license plate number from the response
    license_plate = ''
    for text_detection in response['TextDetections']:
        if text_detection['Type'] == 'LINE':
            # debug
            # print(text_detection['DetectedText'])
            # Use regular expressions to find the license plate number
            match = re.search(r'^(?=(?:[^A-Z]*[A-Z]){3})(?=(?:\D*\d){4})[A-Z0-9]{7}$', text_detection['DetectedText'])
            if match:
                license_plate = match.group(0)
                print("License plate: ", license_plate)
                break

    #grab s3 tags
    #create s3 client
    s3 = boto3.client('s3')
        
    # Retrieve the tags for the object to add to json passed to downward queue
    response = s3.get_object_tagging(Bucket=bucket, Key=key)

    # Get the list of tags from the response
    tags = response['TagSet']

    # Initialize variables for tag values
    tagType = None
    tagDateTime = None
    tagLocation = None

    # Iterate over the tags and find the desired ones
    for tag in tags:
        if tag['Key'] == 'Type':
            tagType = tag['Value']
        elif tag['Key'] == 'DateTime':
            tagDateTime = tag['Value']
        elif tag['Key'] == 'Location':
            tagLocation = tag['Value']


    # Check the format of the license plate number
    if len(license_plate) != 7:
        
        # *****************    new     ***********************
            # Specify the AWS region where your EventBridge bus is located
        region = 'us-west-2'

        # Create an instance of the EventBridge client
        event_bridge_client = boto3.client('events', region_name=region)

        # Prepare the sample event payload
        event_payload = {
            'LicensePlateText': response,
            'Location': tagLocation,
            'DateTime': tagDateTime,
            'Type': tagType
        }

        # Create an instance of the PutEventsRequest and add the event to it
        request = {
            'Entries': [
                {

                    'Detail': event_payload
                }
            ]
        }

        try:
            # Send the event to the EventBridge bus
            response = event_bridge_client.put_events(Entries=request['Entries'])

            # Check the response for any failed events
            for entry in response['Entries']:
                if 'ErrorCode' in entry:
                    print(f"Failed to send event: {entry['ErrorCode']} - {entry['ErrorMessage']}")

            print("Event sent successfully!")
        except Exception as ex:
            print(f"Error sending event: {str(ex)}")
        
        # *****************    new     ***********************
        
        
        # ****************** previous ************************
        # If the license plate is not 7 characters long, pass it to EventBridge bus
        #event_bridge = boto3.client('events')
        #event_bridge.put_events(
            #Entries=[
                #{
                    #'Source': 'license_plate',
                    #'DetailType': 'invalid_license_plate',
                    #'Detail': f'{{"plate_number": "{license_plate}", "bucket": "{bucket}", "key": "{key}"}}'
                #}
            #]
        #)
        # ****************** previous ***************************
    else:
        # If the license plate is 7 characters long, pass it to the downward queue in JSON format
        sqs = boto3.client('sqs')
        queue_url = 'https://sqs.us-east-1.amazonaws.com/331002970834/P3DownwardQueue'

        # Debug print statements
        print("tagType: " + tagType)
        print("tagDateTime: " + tagDateTime)
        print("tagLocation: " + tagLocation)
        message_body = {
            'plate_number': license_plate,
            'Type': tagType,
            'DateTime': tagDateTime,
            'Location': tagLocation
        }
        sqs.send_message(
            QueueUrl=queue_url,
            MessageBody=json.dumps(message_body)
        )

    return {
        'statusCode': 200,
        'body': 'Text extraction completed'
    }
