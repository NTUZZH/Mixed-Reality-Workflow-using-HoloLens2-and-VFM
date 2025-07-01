# server.py
import argparse
import ast
import base64
import io
import json
from cgi import FieldStorage
from datetime import datetime
from http.server import BaseHTTPRequestHandler, HTTPServer
from model import load_model
from utils.tools import *
from PIL import Image
import traceback
import numpy as np
import cv2


# Parameters
args = argparse.Namespace(
    model_path='./weights/FastSAM-x.pt',
    imgsz=1024,
    device=torch.device("cuda" if torch.cuda.is_available() else "cpu"),
    retina=True,
    iou=0.9,
    conf=0.4,
    img_path='FromHolo2',
    output='./output/',
    randomcolor=True,
    point_prompt='[[0,0]]',
    point_label='[0]',
    box_prompt='[0,0,0,0]',
    better_quality=True,
    withContours=True,
    regularShape=''
)

# Load the model
Model = load_model(args.model_path)


class Handler(BaseHTTPRequestHandler):

    def do_POST(self):
        form = FieldStorage(
            fp=self.rfile,
            headers=self.headers,
            environ={
                'REQUEST_METHOD': 'POST',
                'CONTENT_TYPE': self.headers['Content-Type'],
            }
        )

        # Getting the image and no preprocess
        image_data = form['image'].file.read()
        img = Image.open(io.BytesIO(image_data))
        # Save the image to a temporary file
        img_path = './tmp.jpg'
        img.save(img_path)
        print("Image received and saved")

        # Set the image path in the arguments
        args.img_path = img_path

        # Get Value
        box_prompt_hl2 = form.getvalue('bbox')
        point_prompt_hl2 = form.getvalue('pList')
        point_label_hl2 = form.getvalue('pLabel')
        regular_hl2 = form.getvalue('regularShape')
        seedpoints_str = form.getvalue('seedPoints')
        # convert from string to list
        seedpoints = parse_seedpoints(seedpoints_str)

        # calculate the nv and centroid.
        normal, centroid = fit_plane(np.array(seedpoints))

        # print(f"Received box_prompt_HL2: {box_prompt_HL2}")
        # Transfer Value into Python format
        args.box_prompt = ast.literal_eval(box_prompt_hl2)
        args.point_prompt = ast.literal_eval(point_prompt_hl2)
        args.point_label = ast.literal_eval(point_label_hl2)
        args.regularShape = regular_hl2

        # initialization
        processed_img = None
        contour_points = None

        try:
            # Pre-process and Segmentation
            results = Model(
                args.img_path,
                imgsz=args.imgsz,
                device=args.device,
                retina_masks=args.retina,
                iou=args.iou,
                conf=args.conf,
                max_det=300
            )

            # Post-process
            if args.box_prompt[2] != 0 and args.box_prompt[3] != 0:
                annotations = prompt(results, args, box=True)
                annotations = np.array([annotations])
                processed_img, contour_points = fast_process(
                    annotations=annotations,
                    args=args,
                    mask_random_color=args.randomcolor,
                    bbox=convert_box_xywh_to_xyxy(args.box_prompt),
                )

            elif args.point_prompt[0] != [0, 0]:
                results = format_results(results[0], 0)
                annotations = prompt(results, args, point=True)
                # list to numpy
                annotations = np.array([annotations])
                processed_img, contour_points = fast_process(
                    annotations=annotations,
                    args=args,
                    mask_random_color=args.randomcolor,
                    points=args.point_prompt,
                )
            else:
                processed_img, contour_points = fast_process(
                    annotations=results[0].masks.data,
                    args=args,
                    mask_random_color=True,
                )

        except Exception as e:
            print(f"Exception during processing: {e}")

        # Convert the processed image to bytes
        # Send Back to Client
        is_success, im_buf_arr = cv2.imencode(".jpg", processed_img)
        byte_im = im_buf_arr.tobytes()
        # Convert the image bytes to a Base64 string
        image_str = base64.b64encode(byte_im).decode('utf-8')
        # Create a dictionary with the image and the points
        data = {
            'image': image_str,
            'contours': contour_points,
            'normal': normal.tolist(),
            'centroid': centroid.tolist()
        }

        # Convert the dictionary to a JSON string
        json_str = json.dumps(data)
        print("Image processed")
        print(f"Image size: {len(byte_im)} bytes")
        # Send the JSON string
        self.send_response(200)
        self.send_header('Content-Type', 'application/json')
        self.send_header(
            'Request-Timestamp', datetime.utcnow().strftime('%Y-%m-%d %H:%M:%S.%f')[:-3])
        self.end_headers()
        self.wfile.write(json_str.encode('utf-8'))
        print(f"Sending {len(json_str)} bytes back to client")


def parse_seedpoints(seedpoints_str):
    points_list = seedpoints_str.split(';')
    points = [(float(p.split(',')[0]), float(p.split(',')[1]),
               float(p.split(',')[2])) for p in points_list]
    return points

# fed data shall be the NumPy array format


def fit_plane(points):
    # Average points
    centroid = np.mean(points, axis=0)
    # Centralize the point, make the 0 mean value
    centered_points = points - centroid
    u, s, vh = np.linalg.svd(centered_points)
    normal = vh[-1]
    return normal, centroid


def prompt(results, args, box=None, point=None):
    try:
        ori_img = cv2.imread(args.img_path)
        ori_h = ori_img.shape[0]
        ori_w = ori_img.shape[1]
        if box:
            mask, idx = box_prompt(
                results[0].masks.data,
                convert_box_xywh_to_xyxy(args.box_prompt),
                ori_h,
                ori_w,
            )

          #  cv2.imshow('Mask', mask)
         #   cv2.waitKey(0)
           # cv2.destroyAllWindows()

        elif point:
            mask, idx = point_prompt(
                results,
                args.point_prompt,
                args.point_label,
                ori_h,
                ori_w
            )
        else:
            return None

        return mask

    except Exception as e:
        print(f"Exception occurred: {e}")
        traceback.print_exc()


if __name__ == "__main__":
    server_address = ('', 50000)
    httpd = HTTPServer(server_address, Handler)
    print(f'Server running on port {server_address[1]}...')
    httpd.serve_forever()
