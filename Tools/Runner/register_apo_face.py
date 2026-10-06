"""Read-only landmark registration. Produces UV projection metadata, never edits images."""
import sys,pathlib,json
ROOT=pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'.codex_tmp/apo-detail-20261006/python'))
import cv2,numpy as np
original=cv2.imread(str(ROOT/'ArtDirection/CharacterReferences/AbdullahEkinci_Reference_v1.png'))
detail=cv2.imread(str(ROOT/'ArtDirection/CharacterReferences/AbdullahEkinci_FaceDetail_v3.png'))
# Enlarge only an in-memory analysis buffer; project the untouched source PNG in Blender.
crop=original[50:300,530:712]
analysis=cv2.resize(crop,None,fx=4,fy=4,interpolation=cv2.INTER_CUBIC)
sift=cv2.SIFT_create(contrastThreshold=.015)
k1,d1=sift.detectAndCompute(cv2.cvtColor(analysis,cv2.COLOR_BGR2GRAY),None)
k2,d2=sift.detectAndCompute(cv2.cvtColor(detail,cv2.COLOR_BGR2GRAY),None)
matches=cv2.BFMatcher().knnMatch(d1,d2,k=2)
good=[a for a,b in matches if a.distance<.78*b.distance]
p=np.float32([((k1[m.queryIdx].pt[0]/4)+530,(k1[m.queryIdx].pt[1]/4)+50) for m in good])
q=np.float32([k2[m.trainIdx].pt for m in good])
affine,mask=cv2.estimateAffinePartial2D(p,q,method=cv2.RANSAC,ransacReprojThreshold=9,maxIters=10000)
if affine is None:raise RuntimeError('No reliable face registration.')
kept=mask[:,0].astype(bool)
error=np.linalg.norm(np.concatenate([p,np.ones((len(p),1))],axis=1)@affine.T-q,axis=1)
report={'original_to_detail':affine.tolist(),'matches':len(good),'inliers':int(kept.sum()),'median_inlier_error_px':float(np.median(error[kept])),
        'source_landmarks':p[kept].tolist(),'detail_landmarks':q[kept].tolist(),'detail_size':[detail.shape[1],detail.shape[0]]}
out=ROOT/'ArtDirection/CharacterModels/ApoDetailedSculpt/face-registration.json'
out.write_text(json.dumps(report,indent=2))
print(json.dumps({k:v for k,v in report.items() if 'landmarks' not in k}))
