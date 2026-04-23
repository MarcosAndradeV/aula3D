import cv2
import mediapipe as mp
from mediapipe.tasks import python
from mediapipe.tasks.python import vision
import socket
import json
import time

# Configuração do UDP
UDP_IP = "127.0.0.1"
UDP_PORT = 5005
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

model_path = 'hand_landmarker.task'

base_options = python.BaseOptions(model_asset_path=model_path)
options = vision.HandLandmarkerOptions(
    base_options=base_options,
    num_hands=1,
    min_hand_detection_confidence=0.5,
    min_hand_presence_confidence=0.5,
    min_tracking_confidence=0.5,
    running_mode=vision.RunningMode.VIDEO,
)

cap = cv2.VideoCapture(0)
cap.set(cv2.CAP_PROP_FRAME_WIDTH, 640)
cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 480)

print(f"Microserviço rodando. Enviando dados para {UDP_IP}:{UDP_PORT}...")

with vision.HandLandmarker.create_from_options(options) as landmarker:
    try:
        while cap.isOpened():
            success, image = cap.read()
            if not success:
                continue

            image = cv2.flip(image, 1)
            h, w, _ = image.shape

            image_rgb = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
            mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=image_rgb)

            timestamp_ms = int(time.time() * 1000)
            results = landmarker.detect_for_video(mp_image, timestamp_ms)

            if results.hand_landmarks:
                hand_landmarks = results.hand_landmarks[0]
                points = []
                
                for landmark in hand_landmarks:
                    points.append({
                        "X": int(landmark.x * w),
                        "Y": int(landmark.y * h)
                    })

                if len(points) == 21:
                    # Desenha bolinhas verdes na mão para feedback visual
                    for p in points:
                        cv2.circle(image, (p["X"], p["Y"]), 4, (0, 255, 0), -1)

                    # Envia os dados para o C#
                    data = json.dumps(points).encode('utf-8')
                    sock.sendto(data, (UDP_IP, UDP_PORT))

            # Exibe a janela da câmara
            cv2.imshow("Olho do MediaPipe (Python)", image)
            
            # O waitKey(1) é OBRIGATÓRIO no OpenCV para que a janela seja atualizada.
            # Se premir ESC com a janela da câmara selecionada, o Python fecha.
            if cv2.waitKey(1) & 0xFF == 27:
                break

    finally:
        cap.release()
        cv2.destroyAllWindows()
        sock.close()