import pathlib,sys,numpy as np
sys.path.insert(0,str(pathlib.Path.cwd()/'Tools/YanYana'))
from author_resident_identity import reference
for name in ('Ada','Derya','Emre'):
    ps,cs,cx,ez,s=reference(name)
    samples=[p.z-ez for p,c in zip(ps,cs) if .025*s<p.z-ez<.16*s and p.y<-.14*s and .026*s<abs(p.x-cx)<.108*s and c.mean()<.29]
    hist,bins=np.histogram(samples,bins=27,range=(.025*s,.16*s))
    print(name,'scale',s,'eyes',ez,'dark_front_bands',[(round(float(bins[i]),4),int(n)) for i,n in enumerate(hist) if n>4],flush=True)
