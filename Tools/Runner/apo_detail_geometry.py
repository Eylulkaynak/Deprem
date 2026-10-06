"""Offline sculpt details for Apo. All surfaces are exported, never made at runtime."""
import math, random
import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree


def enrich(g):
    V, F = g['V'], g['F']
    add, wp, S = g['add'], g['wp'], g['S']
    skin, shirt, hair = g['SKIN'], g['SHIRT'], g['HAIR']
    make_mat = g['material']
    fabric = make_mat('Folded cotton and stitching', '#A4BEDB', .83)
    button = make_mat('Mother of pearl blue buttons', '#B9CDDF', .34)
    thread = make_mat('Cotton seam thread', '#B3C9DD', .81)
    leather = make_mat('Watch leather', '#252525', .54)
    steel = make_mat('Brushed watch case', '#919698', .23)
    dial = make_mat('Watch dial', '#202A36', .29)
    silver = make_mat('Watch index and buckle', '#CAD1D0', .27)
    locks = make_mat('Sculpted swept locks', '#252221', .48)
    beard = make_mat('Trimmed beard fibers', '#443429', .72)
    # Very shallow continuous scalp relief avoids a smooth helmet silhouette.
    for i,raw in enumerate(V):
        p=Vector(raw);py=g['FLOOR']-p.z/S
        angle=math.atan2(abs(p.x),-p.y)
        line=98+60*abs(math.sin(angle))**3+78*max(0,-math.cos(angle))
        fade=g['smooth']((line-py)/9)
        if fade>0 and py<190 and g['WEIGHT'][i].get('Head',0)>.9:
            normal=Vector((p.x,p.y,(p.z-1.705)*.75)).normalized()
            relief=(.0033*(.5+.5*math.cos(angle*9+p.z*17+angle*angle))**4+.00035*math.sin(angle*65+p.z*85+angle*angle*2))*fade
            relief*=g['smooth']((math.hypot(p.x,p.y)-.012)/.04)
            V[i]=tuple(p+normal*relief)
        if abs(p.x)>.124 and abs(p.z-wp(620,190)[1])<.043 and p.y<.015:
            p=Vector(V[i]);p.y+=.007*g['gauss'](abs(p.x),.145,.013)*g['gauss'](p.z,wp(620,190)[1],.025)
            ear_radius=math.sqrt(((abs(p.x)-.135)/.016)**2+((p.z-wp(620,190)[1])/.036)**2)
            p.y-=.002*g['gauss'](ear_radius,.85,.16)
            V[i]=tuple(p)
    surface = BVHTree.FromPolygons([Vector(v) for v in V], F)

    def front(x, z, offset=0):
        hit, normal, _, _ = surface.ray_cast(Vector((x, -.65, z)), Vector((0, 1, 0)), 1.3)
        if hit is None:
            loc, normal, _, _ = surface.find_nearest(Vector((x, -.1, z)))
            return loc+normal*offset
        return Vector((x, hit.y-offset, z))

    def nearest(point, offset=0):
        p, n, _, _ = surface.find_nearest(Vector(point))
        return p+n*offset, n

    def tube(name, points, radius, mat, bone='Head', proj=0, seg=8, flattened=1):
        pts = [Vector(p) for p in points]
        vs, fs = [], []
        for j,p in enumerate(pts):
            t = (pts[min(j+1,len(pts)-1)]-pts[max(0,j-1)]).normalized()
            normal = Vector((0,-1,0))
            if abs(t.dot(normal))>.96: normal=Vector((0,0,1))
            u=t.cross(normal).normalized(); n=u.cross(t).normalized()
            r=radius[j] if isinstance(radius,list) else radius
            for k in range(seg):
                a=math.tau*k/seg;vs.append(p+u*(math.cos(a)*r)+n*(math.sin(a)*r*flattened))
        for j in range(len(pts)-1):
            for k in range(seg):fs.append((j*seg+k,j*seg+(k+1)%seg,(j+1)*seg+(k+1)%seg,(j+1)*seg+k))
        fs += [tuple(reversed(range(seg))), tuple((len(pts)-1)*seg+k for k in range(seg))]
        add(name,vs,fs,mat,bone,[proj]*len(vs))

    def ellipsoid(name, center, radii, mat, bone='Head', proj=0, seg=28, rows=16):
        vs,fs=[],[];c=Vector(center)
        for j in range(rows+1):
            ph=math.pi*j/rows
            for i in range(seg):
                a=math.tau*i/seg
                vs.append(c+Vector((radii[0]*math.sin(ph)*math.cos(a),radii[1]*math.sin(ph)*math.sin(a),radii[2]*math.cos(ph))))
        for j in range(rows):
            for i in range(seg):fs.append((j*seg+i,j*seg+(i+1)%seg,(j+1)*seg+(i+1)%seg,(j+1)*seg+i))
        add(name,vs,fs,mat,bone,[proj]*len(vs))

    # Anterior eye surfaces have an almond contour and a convex corneal profile.
    # The source painting is the iris/albedo, not a substitute for eye volume.
    for cx in (588,649):
        x, z = wp(cx,171);vs=[];fs=[];N=80;R=18
        for j in range(R+1):
            r=max(.0001,j/R)
            for i in range(N):
                a=math.tau*i/N
                xx=x+18.4*S*r*math.cos(a)
                zz=z+(11.4 if math.sin(a)>0 else 9.2)*S*r*math.sin(a)
                p=front(xx,zz,-.0008+.0118*(1-r*r)**2)
                vs.append(p)
        for j in range(R):
            for i in range(N):fs.append((j*N+i,j*N+(i+1)%N,(j+1)*N+(i+1)%N,(j+1)*N+i))
        add('Convex eye '+str(cx),vs,fs,g['EYES'],'Head',[1]*len(vs))
    # A small number of low relief fibers articulate the beard without a shaggy outline.
    import numpy as np
    im=g['image'];px=np.array(im.pixels[:],dtype=np.float32).reshape(im.size[1],im.size[0],4)
    rng=random.Random(104)
    made=0
    for _ in range(1600):
        ix=rng.uniform(551,690);iy=rng.uniform(213,286)
        rgb=px[im.size[1]-1-int(iy*im.size[1]/1254),int(ix*im.size[0]/1254),:3]
        if float(rgb.mean())>.24 or (abs(ix-620)<39 and 220<iy<249):continue
        x,z=wp(ix,iy);p=front(x,z,.0003)
        if p.y>-.060 or abs(p.x-x)>.001 or abs(p.z-z)>.001:continue
        length=rng.uniform(.003,.006)
        pts=[front(x+(.001*(ix-620)/70)*t,z-length*t,-.0002) for t in (0,.33,.67,1)]
        tube('Trimmed beard relief',pts,[.00010,.00030,.00027,.00006],beard,proj=1,seg=6,flattened=.35)
        made+=1
        if made>=170:break

    # Layered, rolled collar with a real folded edge and a thin underside.
    def panel(name,outline,mat,bone,thickness=.0025):
        # Bilinear quad with a soft raised fold across its width.
        a,b,c,d=[Vector(v) for v in outline];vs=[];fs=[];N=14
        for layer in (0,1):
            for j in range(N+1):
                v=j/N
                for i in range(N+1):
                    u=i/N;p=a*(1-u)*(1-v)+b*u*(1-v)+c*u*v+d*(1-u)*v
                    p.y-=.004*math.sin(math.pi*u)*math.sin(math.pi*v);p.y+=layer*thickness
                    vs.append(p)
        M=(N+1)**2
        for layer in (0,1):
            for j in range(N):
                for i in range(N):
                    q=layer*M+j*(N+1)+i;f=(q,q+1,q+N+2,q+N+1);fs.append(tuple(reversed(f)) if layer else f)
        ring=list(range(N+1))+[j*(N+1)+N for j in range(1,N+1)]+[N*(N+1)+i for i in range(N-1,-1,-1)]+[j*(N+1) for j in range(N-1,0,-1)]
        for i,q in enumerate(ring):r=ring[(i+1)%len(ring)];fs.append((q,r,r+M,q+M))
        add(name,vs,fs,mat,bone,[1]*len(vs))
        return [Vector(x) for x in outline]

    for side in (-1,1):
        outline=[front(side*x,z,.002) for x,z in ((.073,1.447),(.124,1.425),(.121,1.390),(.091,1.427))]
        outline=panel('Folded shirt collar',outline,fabric,'Chest')
        # The rolled edge is part of the lapel surface; do not overlay a
        # nearly coplanar ring that produces contact aliasing at portrait scale.
        tag='L' if side>0 else 'R'
        # A cuff is a closed ring, with the wrist emerging from its opening.
        cuffx=side*.408;vs=[];fs=[];N=64
        for j in range(4):
            z=.837+j*.008
            for i in range(N):
                a=math.tau*i/N;vs.append((cuffx+.049*math.cos(a),.059*math.sin(a),z))
        for j in range(3):
            for i in range(N):fs.append((j*N+i,j*N+(i+1)%N,(j+1)*N+(i+1)%N,(j+1)*N+i))
        add('Tailored cuff',vs,fs,fabric,'Forearm.'+tag,[0]*len(vs))
        for zz in (.840,.858):
            pts=[Vector((cuffx+.0493*math.cos(math.tau*j/64),.0593*math.sin(math.tau*j/64),zz)) for j in range(65)]
            tube('Cuff stitching',pts,.0006,thread,'Forearm.'+tag,seg=6)
        ellipsoid('Cuff button',(cuffx,-.061,.849),(.0034,.0013,.0034),button,'Forearm.'+tag,seg=20,rows=10)

    # Raised placket, its two stitch lines, and individually seated four-hole buttons.
    for side in (-1,1):
        pts=[front(side*.0078,z,.0011) for z in [1.350-i*.455/80 for i in range(81)]]
        tube('Placket seam',pts,.00065,thread,'Chest',seg=6)
    for py in (363,428,498,571,631):
        x,z=wp(619,py);p=front(x,z,.0020)
        ellipsoid('Shirt four-hole button',p,(.0049,.0018,.0049),button,'Chest',seg=24,rows=12)
        for dx in (-.0011,.0011):
            for dz in (-.0011,.0011):ellipsoid('Button hole',p+Vector((dx,-.0018,dz)),(.00050,.00035,.00050),shirt,'Chest',seg=10,rows=6)

    # Wristwatch: strap, polished bezel, recessed dial, indices and two hands.
    cx=.409;cz=.822
    pts=[(cx+.047*math.cos(math.tau*j/64),.054*math.sin(math.tau*j/64),cz) for j in range(65)]
    tube('Leather watch strap',pts,.0068,leather,'Hand.L',seg=10)
    center=Vector((cx,-.057,cz))
    ellipsoid('Watch case',center,(.020,.006,.022),steel,'Hand.L')
    ellipsoid('Watch dark face',center+Vector((0,-.005,0)),(.016,.002,.018),dial,'Hand.L')
    for i in range(12):
        a=math.tau*i/12;p=center+Vector((math.sin(a)*.0128,-.0074,math.cos(a)*.0145))
        ellipsoid('Watch hour marker',p,(.0008,.0004,.0012),silver,'Hand.L',seg=10,rows=6)
    tube('Watch hour hand',[center+Vector((0,-.008,0)),center+Vector((-.007,-.008,.006))],.0007,silver,'Hand.L',seg=6)
    tube('Watch minute hand',[center+Vector((0,-.008,0)),center+Vector((.009,-.008,.010))],.0005,silver,'Hand.L',seg=6)

    # Real belt geometry wraps around the waist, including loops and a buckle.
    vs=[];fs=[];N=128
    for j in range(4):
        z=.850+j*.014
        for i in range(N):
            a=math.tau*i/N;vs.append((.235*math.cos(a),.147*math.sin(a),z))
    for j in range(3):
        for i in range(N):fs.append((j*N+i,j*N+(i+1)%N,(j+1)*N+(i+1)%N,(j+1)*N+i))
    add('Leather belt',vs,fs,leather,'Hips',[0]*len(vs))
    pts=[(-.035,-.151,.837),(.035,-.151,.837),(.035,-.151,.884),(-.035,-.151,.884),(-.035,-.151,.837)]
    tube('Belt buckle frame',pts,.0025,silver,'Hips',seg=10)
    tube('Buckle pin',[(-.031,-.153,.860),(.019,-.153,.860)],.0014,steel,'Hips',seg=8)
    for x in (-.173,-.088,.088,.173):
        pts=[front(x,z,.005) for z in (.852,.859,.876,.896)]
        tube('Belt loop',pts,.006,g['PANTS'],'Hips',seg=8,flattened=.38)

    # Crossed laces and toe-cap seams are actual geometry on each shaped shoe.
    lace=make_mat('Woven dark laces','#3A3A3B',.78)
    def shoe_top(x,y,offset=.0008):
        hit,n,_,_=surface.ray_cast(Vector((x,y,.29)),Vector((0,0,-1)),.30)
        return (hit+n*offset) if hit is not None else Vector((x,y,.10))
    for side in (-1,1):
        cx=wp(620+side*103,1144)[0];tag='L' if side>0 else 'R'
        for k in range(4):
            yy=-.021+k*.014
            for flip in (-1,1):
                pts=[shoe_top(cx+flip*(-.019+.038*t),yy+.009*t,.0014) for t in [j/12 for j in range(13)]]
                tube('Crossed shoe lace',pts,.0012,lace,'Foot.'+tag,seg=8)
        pts=[shoe_top(cx+.067*math.cos(a),-.073+.014*math.sin(a),.0008) for a in [math.pi*j/32 for j in range(33)]]
        tube('Leather toe-cap seam',pts,.00065,leather,'Foot.'+tag,seg=6)

    return {'continuous_sculpted_hair_flow':True,'beard_relief_fibers':made,'convex_eyes':2,'folded_collars':2,
            'physical_buttons':7,'shoe_laces':True,'watch_parts':'strap, case, dial, 12 indices, hands','geometry_details':True}
