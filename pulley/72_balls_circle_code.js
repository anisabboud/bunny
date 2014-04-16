// Convenience Declarations For Dependencies.
// 'Core' Is Configured In Libraries Section.
// Some of these may not be used by this example.
var Conversions = Core.Conversions;
var Debug = Core.Debug;
var Path2D = Core.Path2D;
var Point2D = Core.Point2D;
var Point3D = Core.Point3D;
var Matrix2D = Core.Matrix2D;
var Matrix3D = Core.Matrix3D;
var Mesh3D = Core.Mesh3D;
var Plugin = Core.Plugin;
var Tess = Core.Tess;
var Sketch2D = Core.Sketch2D;
var Solid = Core.Solid;
var Vector2D = Core.Vector2D;
var Vector3D = Core.Vector3D;

// Template Code:
params = [
    { "id": "r", "displayName": "Radius", "type": "length", "rangeMin": 1.0, "rangeMax": 100.0, "default": 10.0 }
];

function process(params) {
    var r = 1.8;
    var R = 49.6;
    var balls = 72;
    var ndivs = Tess.circleDivisions(r);
    var tau = 0.8506508084;
    var one = 0.5257311121;
    var lod = 0;

    while(ndivs > 6){
        lod++;
        ndivs /= 2;
    }

    Debug.log(Tess.circleDivisions(r) + " " + lod);

    var v = [
        [ tau, one, 0.0 ],
        [-tau, one, 0.0 ],
        [-tau,-one, 0.0 ],
        [ tau,-one, 0.0 ],
        [ one, 0.0, tau ],
        [ one, 0.0,-tau ],
        [-one, 0.0,-tau ],
        [-one, 0.0, tau ],
        [ 0.0, tau, one ],
        [ 0.0,-tau, one ],
        [ 0.0,-tau,-one ],
        [ 0.0, tau,-one ]
    ];
    
    var tris = [
        [ v[4], v[8], v[7] ],
        [ v[4], v[7], v[9] ],
        [ v[5], v[6],v[11] ],
        [ v[5],v[10], v[6] ],
        [ v[0], v[4], v[3] ],
        [ v[0], v[3], v[5] ],
        [ v[2], v[7], v[1] ],
        [ v[2], v[1], v[6] ],
        [ v[8], v[0],v[11] ],
        [ v[8],v[11], v[1] ],
        [ v[9],v[10], v[3] ],
        [ v[9], v[2],v[10] ],
        [ v[8], v[4], v[0] ],
        [v[11], v[0], v[5] ],
        [ v[4], v[9], v[3] ],
        [ v[5], v[3],v[10] ],
        [ v[7], v[8], v[1] ],
        [ v[6], v[1],v[11] ], 
        [ v[7], v[2], v[9] ],
        [ v[6],v[10], v[2] ]
    ];
    
    function norm(a) {
        var l = Math.sqrt(a[0]*a[0] + a[1]*a[1] + a[2]*a[2]);
        return [ a[0]/l, a[1]/l, a[2]/l ];
    }

    function sum(a, b) {
        return [ (a[0]+b[0]), (a[1]+b[1]), (a[2]+b[2]) ];
    }

    function xf(a, s, index) {
      var angle = 2 * Math.PI / balls * index;
      var dx = R * Math.cos(angle);
      var dy = R * Math.sin(angle);
      return [ a[0]*s + dx, a[1]*s + dy, a[2]*s + s];
    }
    
    for (var i = 0; i < lod; i++) {
        var ntris = [];
        for(var j = 0; j < tris.length; j++) {
            var ma = norm(sum(tris[j][0], tris[j][1]));
            var mb = norm(sum(tris[j][1], tris[j][2]));
            var mc = norm(sum(tris[j][2], tris[j][0]));
 
            ntris.push([tris[j][0], ma, mc]);
            ntris.push([tris[j][1], mb, ma]);
            ntris.push([tris[j][2], mc, mb]);
            ntris.push([ma, mb, mc]);
        }
        tris = ntris;
    }

    var mesh = new Mesh3D();
    for (var index = 0; index < balls; index++) {
      for (var i = 0; i < tris.length; i++) {
          mesh.triangle(xf(tris[i][0],r, index), xf(tris[i][1],r, index), xf(tris[i][2],r, index));
      }
    }

    var solid = Solid.make(mesh);

    return solid;
}